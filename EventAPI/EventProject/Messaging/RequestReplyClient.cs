using EventAPI.DTO.Messaging;
using EventAPI.DTO.Shared;
using EventAPI.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace EventAPI.Messaging
{
    public interface IRequestReplyClient
    {
        Task<LecturerValidationResponse> ValidateLecturerAsync(
            int lecturerId,
            CancellationToken ct = default);
    }

    public class RequestReplyClient : IRequestReplyClient, IAsyncDisposable
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<string>> _pending = new();
        private readonly RabbitMqConsumerOptions _options;
        private readonly ILogger<RequestReplyClient> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private IConnection? _connection;
        private IChannel? _channel;
        public IChannel? Channel => _channel;

        public const string ReplyQueue = RoutingKeys.LecturerReplyQueue;

        private const string RequestQueue = RoutingKeys.LecturerRequestQueue;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);


        public RequestReplyClient(IOptions<RabbitMqConsumerOptions> options, ILogger<RequestReplyClient> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<LecturerValidationResponse> ValidateLecturerAsync(int lecturerId, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var correlationId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[correlationId] = tcs;

            var request = new LecturerValidationRequest(correlationId, lecturerId, ReplyQueue);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            var props = new BasicProperties
            {
                Persistent = false,
                MessageId = correlationId.ToString(),
                CorrelationId = correlationId.ToString(),
                ReplyTo = ReplyQueue,
                ContentType = "application/json"
            };

            await _channel!.BasicPublishAsync(
                exchange: "",
                routingKey: RequestQueue,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            _logger.LogDebug("Poslat validation request za LecturerId={Id}, CorrelationId={CId}", lecturerId, correlationId);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(Timeout);
                var responseJson = await tcs.Task.WaitAsync(cts.Token);
                return JsonSerializer.Deserialize<LecturerValidationResponse>(responseJson)!;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"LecturerService nije odgovorio u roku od {Timeout.TotalSeconds}s za LecturerId={lecturerId}");
            }
            finally
            {
                _pending.TryRemove(correlationId, out _);
            }
        }

        public void Complete(Guid correlationId, string responseJson)
        {
            if (_pending.TryGetValue(correlationId, out var tcs))
                tcs.TrySetResult(responseJson);
            else
                _logger.LogWarning("Stigao odgovor za nepoznati CorrelationId {CId}", correlationId);
        }

        public async Task EnsureInitializedAsync(CancellationToken ct)
        {
            if (_channel is not null && _channel.IsOpen) return;

            await _initLock.WaitAsync(ct);
            try
            {
                if (_channel is not null && _channel.IsOpen) return;

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

                await _channel.QueueDeclareAsync(ReplyQueue, durable: false, exclusive: false, autoDelete: true, cancellationToken: ct);
                await _channel.QueueDeclareAsync(RequestQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null) await _channel.DisposeAsync();
            if (_connection is not null) await _connection.DisposeAsync();
            _initLock.Dispose();
        }
    }
}
