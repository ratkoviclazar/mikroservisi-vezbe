using EventAPI.DTO.Messaging;
using EventAPI.DTO.Shared;
using EventProject.LecturerService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Lecturer_Service.HostedServices
{
    public class ValidationRequestConsumerHostedService : BackgroundService
    {
        private const string RequestQueue = RoutingKeys.LecturerRequestQueue;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<ValidationRequestConsumerHostedService> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public ValidationRequestConsumerHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMqOptions> options,
            ILogger<ValidationRequestConsumerHostedService> logger)
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

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(RequestQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<LecturerValidationRequest>(json);

                if (request is null)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    return;
                }

                _logger.LogDebug("Validation request za LecturerId={Id}", request.LecturerId);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LecturerDbContext>();

                var lecturer = await db.Lecturers
                    .Where(l => l.Id == request.LecturerId)
                    .Select(l => new { l.Name, l.Surname })
                    .FirstOrDefaultAsync(stoppingToken);

                var response = new LecturerValidationResponse(
                    CorrelationId: request.CorrelationId,
                    Exists: lecturer is not null,
                    FullName: lecturer is not null ? $"{lecturer.Name} {lecturer.Surname}" : null);

                var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

                var props = new BasicProperties
                {
                    Persistent = false,
                    CorrelationId = request.CorrelationId.ToString(),
                    ContentType = "application/json"
                };

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: request.ReplyTo,
                    mandatory: false,
                    basicProperties: props,
                    body: responseBody,
                    cancellationToken: stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                _logger.LogDebug("Reply poslat za CorrelationId={CId}, Exists={E}", request.CorrelationId, response.Exists);
            };

            await _channel.BasicConsumeAsync(RequestQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
        }

        public override async Task StopAsync(CancellationToken ct)
        {
            if (_channel is not null) await _channel.CloseAsync(ct);
            if (_connection is not null) await _connection.CloseAsync(ct);
            await base.StopAsync(ct);
        }
    }
}
