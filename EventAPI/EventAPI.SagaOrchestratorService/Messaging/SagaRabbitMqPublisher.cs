using EventAPI.SagaOrchestratorService.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EventAPI.SagaOrchestratorService.Messaging
{
    public class SagaRabbitMqPublisher : ISagaMessagePublisher
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<SagaRabbitMqPublisher> _logger;

        public SagaRabbitMqPublisher(
            IOptions<RabbitMqOptions> options,
            ILogger<SagaRabbitMqPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task PublishAsync<TMessage>(
            TMessage message,
            string exchange,
            string routingKey,
            CancellationToken ct = default)
            where TMessage : class
        {
            var json = JsonSerializer.Serialize(message);

            await PublishRawAsync(
                exchange,
                json,
                typeof(TMessage).Name,
                routingKey,
                ct);
        }

        public async Task PublishRawAsync(
            string exchange,
            string payload,
            string messageType,
            string routingKey,
            CancellationToken ct = default)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            await using var connection = await factory.CreateConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            var body = Encoding.UTF8.GetBytes(payload);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = messageType,
                MessageId = Guid.NewGuid().ToString()
            };

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);

            _logger.LogInformation(
                "Saga message {MessageType} published. Exchange={Exchange}, RoutingKey={RoutingKey}",
                messageType,
                exchange,
                routingKey);
        }
    }
}
