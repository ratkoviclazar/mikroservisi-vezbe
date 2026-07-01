using Catalog_Service.Messaging;
using EventAPI.DTO.Messaging.Saga.Choreography;
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
    public class ChoreographyLocationReservationCancelConsumer : BackgroundService
    {
        private const string QueueName = RoutingKeys.ChoreographyLocationReservationCancelRequestQueue;

        private readonly RabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChoreographyLocationReservationCancelConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public ChoreographyLocationReservationCancelConsumer(
            IOptions<RabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<ChoreographyLocationReservationCancelConsumer> logger)
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
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: _options.Exchange,
                routingKey: RoutingKeys.LocationReservationCancelRequested,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var message = JsonSerializer.Deserialize<LocationReservationCancelRequested>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (message is null)
                        throw new InvalidOperationException("LocationReservationCancelRequested poruka nije validna.");

                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();

                    var reservation = await db.LocationReservations
                        .FirstOrDefaultAsync(x => x.Id == message.ReservationId, stoppingToken);

                    if (reservation is null)
                    {
                        _logger.LogWarning(
                            "Reservation for compensation was not found. SagaId={SagaId}, ReservationId={ReservationId}",
                            message.SagaId,
                            message.ReservationId);

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        return;
                    }

                    if (!reservation.IsCancelled)
                    {
                        reservation.IsCancelled = true;
                        reservation.CancelReason = message.Reason;
                        reservation.CancelledAt = DateTime.UtcNow;

                        await db.SaveChangesAsync(stoppingToken);
                    }

                    var cancelled = new LocationReservationCancelled
                    {
                        SagaId = message.SagaId,
                        CorrelationId = message.CorrelationId,
                        EventId = message.EventId,
                        ReservationId = message.ReservationId,
                        NewLocationId = message.NewLocationId,
                        Reason = message.Reason
                    };

                    await PublishAsync(
                        cancelled,
                        RoutingKeys.LocationReservationCancelled,
                        nameof(LocationReservationCancelled),
                        stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Location reservation cancelled. SagaId={SagaId}, EventId={EventId}, ReservationId={ReservationId}, Reason={Reason}",
                        message.SagaId,
                        message.EventId,
                        message.ReservationId,
                        message.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom kompenzacije rezervacije lokacije. Body: {Body}",
                        json);

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
                "ChoreographyLocationReservationCancelConsumer started. Queue={Queue}, RoutingKey={RoutingKey}",
                QueueName,
                RoutingKeys.LocationReservationCancelRequested);
        }

        private async Task PublishAsync<T>(
            T message,
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
