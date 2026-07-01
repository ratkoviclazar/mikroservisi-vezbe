using EventAPI.DTO.Messaging.Saga;
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
    public class LecturerValidationSagaConsumer : BackgroundService
    {
        private readonly RabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LecturerValidationSagaConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public LecturerValidationSagaConsumer(
            IOptions<RabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<LecturerValidationSagaConsumer> logger)
        {
            _options = options.Value;
            _scopeFactory = scopeFactory;
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
                exchange: _options.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: RoutingKeys.SagaLecturerValidateRequestQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: RoutingKeys.SagaLecturerValidateRequestQueue,
                exchange: _options.Exchange,
                routingKey: RoutingKeys.SagaLecturerValidateRequestQueue,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: RoutingKeys.SagaReplyQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: RoutingKeys.SagaReplyQueue,
                exchange: _options.Exchange,
                routingKey: RoutingKeys.SagaReplyQueue,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var command = JsonSerializer.Deserialize<ValidateLecturerCommand>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (command is null)
                        throw new InvalidOperationException("ValidateLecturerCommand nije validan.");

                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<LecturerDbContext>();

                    var lecturer = await db.Lecturers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == command.LecturerId, stoppingToken);

                    var lecturerExists = lecturer is not null;

                    var reply = new LecturerValidatedReply
                    {
                        SagaId = command.SagaId,
                        CorrelationId = command.CorrelationId,
                        Success = lecturerExists,
                        LecturerExists = lecturerExists,
                        FullName = lecturerExists
                            ? lecturer!.Name + " " + lecturer!.Surname
                            : null,
                        ErrorMessage = lecturerExists
                            ? null
                            : "Predavač ne postoji."
                    };

                    await PublishReplyAsync(
                        reply,
                        command.ReplyTo,
                        stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Saga lecturer validation processed. SagaId={SagaId}, LecturerId={LecturerId}, LecturerExists={LecturerExists}",
                        command.SagaId,
                        command.LecturerId,
                        lecturerExists);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom obrade Saga lecturer validacije. Body: {Body}",
                        json);

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: RoutingKeys.SagaLecturerValidateRequestQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "LecturerValidationSagaConsumer started. Queue={Queue}, Exchange={Exchange}",
                RoutingKeys.SagaLecturerValidateRequestQueue,
                _options.Exchange);
        }

        private async Task PublishReplyAsync(
            LecturerValidatedReply reply,
            string? replyTo,
            CancellationToken ct)
        {
            if (_channel is null)
                throw new InvalidOperationException("RabbitMQ channel nije inicijalizovan.");

            var routingKey = string.IsNullOrWhiteSpace(replyTo)
                ? RoutingKeys.SagaReplyQueue
                : replyTo;

            var json = JsonSerializer.Serialize(reply);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = nameof(LecturerValidatedReply),
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = reply.CorrelationId.ToString()
            };

            await _channel.BasicPublishAsync(
                exchange: _options.Exchange,
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
