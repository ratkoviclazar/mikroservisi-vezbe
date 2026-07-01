using Catalog_Service.Messaging;
using EventAPI.DTO.Messaging.Saga;
using EventAPI.DTO.Shared;
using EventProject.CatalogService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Catalog_Service.HostedServices
{
    public class ReferenceDataValidationSagaConsumer : BackgroundService
    {
        private readonly RabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReferenceDataValidationSagaConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public ReferenceDataValidationSagaConsumer(
            IOptions<RabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<ReferenceDataValidationSagaConsumer> logger)
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

            await _channel.QueueDeclareAsync(
                queue: RoutingKeys.SagaReferenceValidateRequestQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: RoutingKeys.SagaReferenceValidateRequestQueue,
                exchange: _options.Exchange,
                routingKey: RoutingKeys.SagaReferenceValidateRequestQueue,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var command = JsonSerializer.Deserialize<ValidateReferenceDataCommand>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (command is null)
                        throw new InvalidOperationException("ValidateReferenceDataCommand nije validan.");

                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();

                    var locationExists = await db.Locations
                        .AsNoTracking()
                        .AnyAsync(x => x.Id == command.LocationId, stoppingToken);

                    var eventTypeExists = await db.EventTypes
                        .AsNoTracking()
                        .AnyAsync(x => x.Id == command.EventTypeId, stoppingToken);

                    var reply = new ReferenceDataValidatedReply
                    {
                        SagaId = command.SagaId,
                        CorrelationId = command.CorrelationId,
                        Success = locationExists && eventTypeExists,
                        LocationExists = locationExists,
                        EventTypeExists = eventTypeExists,
                        ErrorMessage = locationExists && eventTypeExists
                            ? null
                            : BuildErrorMessage(locationExists, eventTypeExists)
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
                        "Saga reference validation processed. SagaId={SagaId}, LocationExists={LocationExists}, EventTypeExists={EventTypeExists}",
                        command.SagaId,
                        locationExists,
                        eventTypeExists);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom obrade Saga reference validacije. Body: {Body}",
                        json);

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: RoutingKeys.SagaReferenceValidateRequestQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "ReferenceDataValidationSagaConsumer started. Queue={Queue}, Exchange={Exchange}",
                RoutingKeys.SagaReferenceValidateRequestQueue,
                _options.Exchange);
        }

        private async Task PublishReplyAsync(
            ReferenceDataValidatedReply reply,
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
                Type = nameof(ReferenceDataValidatedReply),
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

        private static string BuildErrorMessage(bool locationExists, bool eventTypeExists)
        {
            var errors = new List<string>();

            if (!locationExists)
                errors.Add("Lokacija ne postoji.");

            if (!eventTypeExists)
                errors.Add("Tip događaja ne postoji.");

            return string.Join(" ", errors);
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
