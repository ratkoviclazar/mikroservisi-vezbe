using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EventAPI.Messaging
{
    public interface IChoreographyRabbitMqPublisher
    {
        Task PublishAsync<T>(
            T message,
            string exchange,
            string routingKey,
            string messageType,
            CancellationToken ct = default);
    }

    public class ChoreographyRabbitMqPublisher : IChoreographyRabbitMqPublisher, IAsyncDisposable
    {
        private readonly SagaRabbitMqOptions _options;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private IConnection? _connection;
        private IChannel? _channel;

        public ChoreographyRabbitMqPublisher(IOptions<SagaRabbitMqOptions> options)
        {
            _options = options.Value;
        }

        public async Task PublishAsync<T>(
            T message,
            string exchange,
            string routingKey,
            string messageType,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            await _channel!.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

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

        private async Task EnsureInitializedAsync(CancellationToken ct)
        {
            if (_channel is not null && _channel.IsOpen)
                return;

            await _initLock.WaitAsync(ct);

            try
            {
                if (_channel is not null && _channel.IsOpen)
                    return;

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost
                };

                _connection = await factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
                await _channel.DisposeAsync();

            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }
}
