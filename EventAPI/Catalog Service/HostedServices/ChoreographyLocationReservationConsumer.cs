using Catalog_Service.Messaging;
using Catalog_Service.Models;
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
    public class ChoreographyLocationReservationConsumer : BackgroundService
    {
        private const string QueueName = "choreography.location.reservation.queue";

        private readonly RabbitMqOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChoreographyLocationReservationConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public ChoreographyLocationReservationConsumer(
            IOptions<RabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<ChoreographyLocationReservationConsumer> logger)
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
                routingKey: RoutingKeys.LocationChangeRequested,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var message = JsonSerializer.Deserialize<LocationChangeRequested>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (message is null)
                        throw new InvalidOperationException("LocationChangeRequested poruka nije validna.");

                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();

                    var locationExists = await db.Locations
                        .AsNoTracking()
                        .AnyAsync(x => x.Id == message.NewLocationId, stoppingToken);

                    if (!locationExists)
                    {
                        var failed = new LocationReservationFailed
                        {
                            SagaId = message.SagaId,
                            CorrelationId = message.CorrelationId,
                            EventId = message.EventId,
                            NewLocationId = message.NewLocationId,
                            ErrorMessage = $"Lokacija sa ID={message.NewLocationId} ne postoji."
                        };

                        await PublishAsync(
                            failed,
                            RoutingKeys.LocationReservationFailed,
                            nameof(LocationReservationFailed),
                            stoppingToken);

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            cancellationToken: stoppingToken);

                        _logger.LogWarning(
                            "Location reservation failed. SagaId={SagaId}, EventId={EventId}, NewLocationId={NewLocationId}",
                            message.SagaId,
                            message.EventId,
                            message.NewLocationId);

                        return;
                    }

                    var reservation = new LocationReservation
                    {
                        SagaId = message.SagaId,
                        CorrelationId = message.CorrelationId,
                        EventId = message.EventId,
                        LocationId = message.NewLocationId,
                        EventDateTime = message.EventDateTime,
                        IsCancelled = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.LocationReservations.Add(reservation);
                    await db.SaveChangesAsync(stoppingToken);

                    var reserved = new LocationReserved
                    {
                        SagaId = message.SagaId,
                        CorrelationId = message.CorrelationId,
                        EventId = message.EventId,
                        OldLocationId = message.OldLocationId,
                        NewLocationId = message.NewLocationId,
                        ReservationId = reservation.Id,
                        EventName = message.EventName,
                        EventDateTime = message.EventDateTime
                    };

                    await PublishAsync(
                        reserved,
                        RoutingKeys.LocationReserved,
                        nameof(LocationReserved),
                        stoppingToken);

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Location reserved. SagaId={SagaId}, EventId={EventId}, ReservationId={ReservationId}, LocationId={LocationId}",
                        message.SagaId,
                        message.EventId,
                        reservation.Id,
                        message.NewLocationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greška prilikom obrade LocationChangeRequested poruke. Body: {Body}",
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
                "ChoreographyLocationReservationConsumer started. Queue={Queue}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                QueueName,
                _options.Exchange,
                RoutingKeys.LocationChangeRequested);
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
