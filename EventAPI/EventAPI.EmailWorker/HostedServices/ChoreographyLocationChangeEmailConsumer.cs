using EventAPI.DTO.Messaging.Saga.Choreography;
using EventAPI.DTO.Shared;
using EventAPI.EmailWorker.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventAPI.EmailWorker.HostedServices
{
    public class ChoreographyLocationChangeEmailConsumer : BackgroundService
    {
        private const string QueueName = "choreography.location.change.email.queue";

        private readonly SagaRabbitMqOptions _options;
        private readonly ILogger<ChoreographyLocationChangeEmailConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public ChoreographyLocationChangeEmailConsumer(
            IOptions<SagaRabbitMqOptions> options,
            ILogger<ChoreographyLocationChangeEmailConsumer> logger
            )
        {
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

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(
                exchange: "event.exchange",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "event.exchange",
                routingKey: RoutingKeys.EventLocationChanged,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                EventLocationChanged? message = null;

                try
                {
                    message = JsonSerializer.Deserialize<EventLocationChanged>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (message is null)
                        throw new InvalidOperationException("EventLocationChanged poruka nije validna.");

                    _logger.LogInformation(
                        "Email notification sent for location change. SagaId={SagaId}, EventId={EventId}, EventName={EventName}, OldLocationId={OldLocationId}, NewLocationId={NewLocationId}",
                        message.SagaId,
                        message.EventId,
                        message.EventName,
                        message.OldLocationId,
                        message.NewLocationId);

                    var sent = new LocationChangeNotificationSent
                    {
                        SagaId = message.SagaId,
                        CorrelationId = message.CorrelationId,
                        EventId = message.EventId,
                        OldLocationId = message.OldLocationId,
                        NewLocationId = message.NewLocationId,
                        EventName = message.EventName,
                        EventDateTime = message.EventDateTime
                    };

                    await PublishAsync(
                        sent,
                        "event.exchange",
                        RoutingKeys.LocationChangeNotificationSent,
                        nameof(LocationChangeNotificationSent),
                        stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom slanja email obaveštenja za promenu lokacije. Body: {Body}",
                        json);

                    if (message is not null)
                    {
                        var failed = new LocationChangeNotificationFailed
                        {
                            SagaId = message.SagaId,
                            CorrelationId = message.CorrelationId,
                            EventId = message.EventId,
                            OldLocationId = message.OldLocationId,
                            NewLocationId = message.NewLocationId,
                            EventName = message.EventName,
                            ErrorMessage = ex.Message
                        };

                        await PublishAsync(
                            failed,
                            "event.exchange",
                            RoutingKeys.LocationChangeNotificationFailed,
                            nameof(LocationChangeNotificationFailed),
                            stoppingToken);

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "ChoreographyLocationChangeEmailConsumer started. Queue={Queue}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                QueueName,
                "event.exchange",
                RoutingKeys.EventLocationChanged);
        }

        private async Task PublishAsync<T>(
            T message,
            string exchange,
            string routingKey,
            string messageType,
            CancellationToken ct)
        {
            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ channel nije inicijalizovan.");

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = messageType,
                MessageId = Guid.NewGuid().ToString()
            };

            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null)
                await _channel.DisposeAsync();

            if (_connection is not null)
                await _connection.DisposeAsync();

            await base.StopAsync(cancellationToken);
        }
    }
}
