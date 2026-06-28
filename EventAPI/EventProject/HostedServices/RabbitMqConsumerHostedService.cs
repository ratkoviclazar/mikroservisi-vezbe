using EventAPI.Messaging;
using EventAPI.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventAPI.HostedServices
{
    public class RabbitMqConsumerHostedService : BackgroundService
    {
        private const int MaxRetries = 10;
        private const string RetryCountHeader = "x-retry-count";
        private const string DeadLetterQueue = "events.dead-letter-queue";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqConsumerOptions _options;
        private readonly ILogger<RabbitMqConsumerHostedService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqConsumerHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMqConsumerOptions> options,
            ILogger<RabbitMqConsumerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Neuspela konekcija ka RabbitMQ-u, pokušaj ponovo za 5s");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            if (_channel is null) return;

            await _channel.QueueDeclareAsync(
                queue: DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            foreach (var binding in _options.Bindings)
            {
                await _channel.ExchangeDeclareAsync(
                    exchange: binding.Exchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                await _channel.QueueDeclareAsync(
                    queue: binding.Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                foreach (var routingKey in binding.RoutingKeys)
                {
                    await _channel.QueueBindAsync(
                        queue: binding.Queue,
                        exchange: binding.Exchange,
                        routingKey: routingKey,
                        cancellationToken: stoppingToken);
                }

                await StartConsumerAsync(binding.Queue, stoppingToken);
            }


            await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
        }

        private async Task StartConsumerAsync(string queueName, CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (sender, ea) =>
            {

                var routingKey = ea.RoutingKey;
                var messageId = Guid.Parse(ea.BasicProperties.MessageId!);
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var retryCount = GetRetryCount(ea.BasicProperties);

                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<MessageDispatcher>();

                try
                {
                    await dispatcher.HandleAsync(messageId, routingKey, json, stoppingToken);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Greška pri obradi poruke {MessageId} sa queue-a {Queue} (routing key: {RoutingKey})",
                        messageId, queueName, routingKey);

                    if (retryCount < MaxRetries)
                    {
                        await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        await RepublishWithRetryAsync(ea, routingKey, json, messageId, retryCount + 1, stoppingToken);
                    }
                    else
                    {
                        _logger.LogError(
                            "Poruka {MessageId} nije obrađena ni nakon {Max} pokušaja — šaljem u DLQ",
                            messageId, MaxRetries);

                        await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        await SendToDeadLetterQueueAsync(ea, json, messageId, routingKey, ex.Message, stoppingToken);
                    }
                }
            };

            await _channel!.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation("Konzument pokrenut za queue {Queue}", queueName);
        }
        private static int GetRetryCount(IReadOnlyBasicProperties props)
        {
            if (props.Headers is null || !props.Headers.TryGetValue(RetryCountHeader, out var val))
                return 0;

            return val switch
            {
                int i => i,
                long l => (int)l,
                byte[] b => int.TryParse(Encoding.UTF8.GetString(b), out var n) ? n : 0,
                _ => Convert.ToInt32(val)
            };
        }
        private async Task RepublishWithRetryAsync(
        BasicDeliverEventArgs ea,
        string routingKey,
        string json,
        Guid messageId,
        int newRetryCount,
        CancellationToken ct)
        {
            var exchange = _options.Bindings
                .FirstOrDefault(b => b.RoutingKeys.Any(rk => MatchesRoutingKey(rk, routingKey)))
                ?.Exchange ?? "";

            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = messageId.ToString(),
                ContentType = "application/json",
                Type = routingKey,
                Headers = new Dictionary<string, object?>
                {
                    [RetryCountHeader] = newRetryCount
                }
            };

            await _channel!.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(json),
                cancellationToken: ct);
        }
        private async Task SendToDeadLetterQueueAsync(
        BasicDeliverEventArgs ea,
        string originalJson,
        Guid messageId,
        string routingKey,
        string errorMessage,
        CancellationToken ct)
        {
            var dlqPayload = JsonSerializer.Serialize(new
            {
                MessageId = messageId,
                OriginalQueue = ea.ConsumerTag,
                RoutingKey = routingKey,
                Payload = originalJson,
                Error = errorMessage,
                FailedAt = DateTime.UtcNow,
                RetryCount = MaxRetries
            });

            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = messageId.ToString(),
                ContentType = "application/json"
            };

            await _channel!.BasicPublishAsync(
                exchange: "",
                routingKey: DeadLetterQueue,
                mandatory: false,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(dlqPayload),
                cancellationToken: ct);

            _logger.LogWarning("Poruka {MessageId} ({RoutingKey}) upisana u DLQ", messageId, routingKey);
        }

        private static bool MatchesRoutingKey(string pattern, string routingKey)
        {
            var patternParts = pattern.Split('.');
            var keyParts = routingKey.Split('.');
            return MatchParts(patternParts, keyParts, 0, 0);
        }

        private static bool MatchParts(string[] pattern, string[] key, int pi, int ki)
        {
            if (pi == pattern.Length && ki == key.Length) return true;
            if (pi == pattern.Length || ki == key.Length) return pattern.ElementAtOrDefault(pi) == "#";
            if (pattern[pi] == "#") return MatchParts(pattern, key, pi + 1, ki) || MatchParts(pattern, key, pi, ki + 1);
            if (pattern[pi] == "*" || pattern[pi] == key[ki]) return MatchParts(pattern, key, pi + 1, ki + 1);
            return false;
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null) await _channel.CloseAsync(cancellationToken);
            if (_connection is not null) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
