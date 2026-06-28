using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace Catalog_Service.Messaging
{
    public interface IRabbitMqPublisher
    {
        Task PublishAsync(Guid messageId, string type, string payload, CancellationToken cancellationToken = default);
    }
    public sealed class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.Exchange))
                throw new InvalidOperationException("RabbitMq:Exchange nije konfigurisan.");

            _factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };
        }

        public async Task PublishAsync(Guid messageId, string type, string payload, CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ kanal nije inicijalizovan.");

            var body = Encoding.UTF8.GetBytes(payload);

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = messageId.ToString(),
                Type = type,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: type,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null && _channel.IsOpen)
                return;

            await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_channel is not null && _channel.IsOpen)
                    return;

                _channel = null;
                _connection = null;

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _options.Exchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: cancellationToken);

                _channel.BasicReturnAsync += async (sender, args) =>
                {
                    _logger.LogWarning(
                        "Poruka {MessageId} se nije mogla rutirati (routing key: {RoutingKey})",
                        args.BasicProperties.MessageId, args.RoutingKey);
                    await Task.CompletedTask;
                };
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_channel is not null) await _channel.DisposeAsync();
                if (_connection is not null) await _connection.DisposeAsync();
            }
            catch { /* swallow on shutdown */ }
            finally
            {
                _initLock.Dispose();
            }
        }
    }
}
