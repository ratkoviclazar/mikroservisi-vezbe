using EventAPI.DTO.Messaging;
using EventAPI.DTO.Shared;
using EventAPI.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EventAPI.Messaging
{
    public interface IEmailPublisher
    {
        Task PublishAsync(EmailMessage email, CancellationToken ct = default);
    }
    public class RabbitMqEmailPublisher : IEmailPublisher, IAsyncDisposable
    {
        private const string EmailQueue = RoutingKeys.EmailQueue;

        private readonly ConnectionFactory _factory;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqEmailPublisher(IOptions<RabbitMqConsumerOptions> options)
        {
            var opt = options.Value;
            _factory = new ConnectionFactory
            {
                HostName = opt.HostName,
                Port = opt.Port,
                UserName = opt.UserName,
                Password = opt.Password,
                VirtualHost = opt.VirtualHost
            };
        }

        public async Task PublishAsync(EmailMessage email, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(email));
            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString(),
                ContentType = "application/json"
            };

            await _channel!.BasicPublishAsync(
                exchange: "",
                routingKey: EmailQueue,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);
        }

        private async Task EnsureInitializedAsync(CancellationToken ct)
        {
            if (_channel is not null && _channel.IsOpen) return;
            await _initLock.WaitAsync(ct);
            try
            {
                if (_channel is not null && _channel.IsOpen) return;
                _connection = await _factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
                await _channel.QueueDeclareAsync(EmailQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            }
            finally { _initLock.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
        }
    }
}
