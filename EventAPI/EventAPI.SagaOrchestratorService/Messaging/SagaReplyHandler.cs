using EventAPI.DTO.Messaging.Saga;
using EventAPI.DTO.Shared;
using EventAPI.SagaOrchestratorService.Data;
using EventAPI.SagaOrchestratorService.Models;
using EventAPI.SagaOrchestratorService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventAPI.SagaOrchestratorService.Messaging
{
    public class SagaReplyHandler : ISagaReplyHandler
    {
        private readonly SagaDbContext _db;
        private readonly ISagaOutboxService _outboxService;
        private readonly ServiceExchangesOptions _exchanges;
        private readonly ILogger<SagaReplyHandler> _logger;

        public SagaReplyHandler(
            SagaDbContext db,
            ISagaOutboxService outboxService,
            IOptions<ServiceExchangesOptions> exchanges,
            ILogger<SagaReplyHandler> logger)
        {
            _db = db;
            _outboxService = outboxService;
            _exchanges = exchanges.Value;
            _logger = logger;
        }

        public async Task HandleAsync(
            string messageType,
            string json,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Saga reply received. Type: {MessageType}. Body: {Json}",
                messageType,
                json);

            switch (messageType)
            {
                case nameof(ReferenceDataValidatedReply):
                    await HandleReferenceDataValidatedAsync(json, ct);
                    break;

                case nameof(LecturerValidatedReply):
                    await HandleLecturerValidatedAsync(json, ct);
                    break;

                case nameof(EventCreatedSagaReply):
                    await HandleEventCreatedAsync(json, ct);
                    break;

                case nameof(EventLectureCreatedSagaReply):
                    await HandleEventLectureCreatedAsync(json, ct);
                    break;

                case nameof(EventDeletedSagaReply):
                    await HandleEventDeletedAsync(json, ct);
                    break;

                default:
                    _logger.LogWarning(
                        "Unsupported saga reply type: {MessageType}",
                        messageType);
                    break;
            }
        }

        private async Task HandleReferenceDataValidatedAsync(
            string json,
            CancellationToken ct)
        {
            var reply = JsonSerializer.Deserialize<ReferenceDataValidatedReply>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (reply is null)
                throw new InvalidOperationException("ReferenceDataValidatedReply nije validan.");

            var saga = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.Id == reply.SagaId, ct);

            if (saga is null)
            {
                _logger.LogWarning(
                    "Saga state not found for SagaId={SagaId}",
                    reply.SagaId);

                return;
            }

            if (!reply.Success)
            {
                saga.Status = SagaStatus.Failed;
                saga.CurrentStep = "ReferenceDataValidationFailed";
                saga.ErrorMessage = reply.ErrorMessage ?? "Referentni podaci nisu validni.";
                saga.FailedAtUtc = DateTime.UtcNow;
                saga.Log += BuildLogLine(
                    $"Reference validation failed. Error={saga.ErrorMessage}");

                await _db.SaveChangesAsync(ct);

                return;
            }

            var correlationId = Guid.NewGuid();

            saga.Status = SagaStatus.ReferenceDataValidated;
            saga.CurrentStep = "ValidateLecturer";
            saga.Log += BuildLogLine("Reference data validated successfully.");

            var command = new ValidateLecturerCommand
            {
                SagaId = saga.Id,
                CorrelationId = correlationId,
                LecturerId = saga.LecturerId ?? 0,
                ReplyTo = RoutingKeys.SagaReplyQueue
            };

            await _outboxService.AddAsync(
                sagaId: saga.Id,
                exchange: _exchanges.LecturerExchange,
                routingKey: RoutingKeys.SagaLecturerValidateRequestQueue,
                message: command,
                ct: ct);

            saga.Log += BuildLogLine(
                $"ValidateLecturerCommand added to outbox. LecturerId={command.LecturerId}.");

            await _db.SaveChangesAsync(ct);
        }

        private async Task HandleLecturerValidatedAsync(
            string json,
            CancellationToken ct)
        {
            var reply = JsonSerializer.Deserialize<LecturerValidatedReply>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (reply is null)
                throw new InvalidOperationException("LecturerValidatedReply nije validan.");

            var saga = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.Id == reply.SagaId, ct);

            if (saga is null)
            {
                _logger.LogWarning(
                    "Saga state not found for SagaId={SagaId}",
                    reply.SagaId);

                return;
            }

            if (!reply.Success)
            {
                saga.Status = SagaStatus.Failed;
                saga.CurrentStep = "LecturerValidationFailed";
                saga.ErrorMessage = reply.ErrorMessage ?? "Predavač nije validan.";
                saga.FailedAtUtc = DateTime.UtcNow;
                saga.Log += BuildLogLine(
                    $"Lecturer validation failed. Error={saga.ErrorMessage}");

                await _db.SaveChangesAsync(ct);

                return;
            }

            saga.Status = SagaStatus.LecturerValidated;
            saga.CurrentStep = "CreateEvent";
            saga.Log += BuildLogLine("Lecturer validated successfully.");

            var command = new CreateEventSagaCommand
            {
                SagaId = saga.Id,
                CorrelationId = Guid.NewGuid(),

                Name = saga.EventName,
                Agenda = saga.EventAgenda,
                DateTime = saga.EventDateTime,
                DurationInHours = saga.EventDurationInHours,
                Price = saga.EventPrice,
                TypeId = saga.TypeId ?? 0,
                LocationId = saga.LocationId ?? 0,

                ReplyTo = RoutingKeys.SagaReplyQueue
            };

            await _outboxService.AddAsync(
                sagaId: saga.Id,
                exchange: _exchanges.EventExchange,
                routingKey: RoutingKeys.SagaEventCreateRequestQueue,
                message: command,
                ct: ct);

            saga.Log += BuildLogLine(
                $"CreateEventSagaCommand added to outbox. Name={command.Name}, TypeId={command.TypeId}, LocationId={command.LocationId}.");

            await _db.SaveChangesAsync(ct);
        }

        private async Task HandleEventCreatedAsync(
            string json,
            CancellationToken ct)
        {
            var reply = JsonSerializer.Deserialize<EventCreatedSagaReply>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (reply is null)
                throw new InvalidOperationException("EventCreatedSagaReply nije validan.");

            var saga = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.Id == reply.SagaId, ct);

            if (saga is null)
            {
                _logger.LogWarning(
                    "Saga state not found for SagaId={SagaId}",
                    reply.SagaId);

                return;
            }

            if (!reply.Success || reply.EventId is null)
            {
                saga.Status = SagaStatus.Failed;
                saga.CurrentStep = "EventCreationFailed";
                saga.ErrorMessage = reply.ErrorMessage ?? "Kreiranje događaja nije uspelo.";
                saga.FailedAtUtc = DateTime.UtcNow;
                saga.Log += BuildLogLine(
                    $"Event creation failed. Error={saga.ErrorMessage}");

                await _db.SaveChangesAsync(ct);

                return;
            }

            saga.EventId = reply.EventId.Value;
            saga.Status = SagaStatus.EventCreated;
            saga.CurrentStep = "CreateEventLecture";
            saga.Log += BuildLogLine(
                $"Event created successfully. EventId={saga.EventId}.");

            if (saga.SimulateLectureCreationFailure)
            {
                saga.Status = SagaStatus.Compensating;
                saga.CurrentStep = "CompensateDeleteEvent";
                saga.ErrorMessage = "Simulirana greška prilikom kreiranja predavanja.";
                saga.Log += BuildLogLine(
                    "Simulated lecture creation failure. DeleteEventSagaCommand will be added to outbox.");

                var deleteEventCommand = new DeleteEventSagaCommand
                {
                    SagaId = saga.Id,
                    CorrelationId = Guid.NewGuid(),
                    EventId = saga.EventId.Value,
                    ReplyTo = RoutingKeys.SagaReplyQueue
                };

                await _outboxService.AddAsync(
                    sagaId: saga.Id,
                    exchange: _exchanges.EventExchange,
                    routingKey: RoutingKeys.SagaEventDeleteRequestQueue,
                    message: deleteEventCommand,
                    ct: ct);

                await _db.SaveChangesAsync(ct);

                return;
            }

            var command = new CreateEventLectureSagaCommand
            {
                SagaId = saga.Id,
                CorrelationId = Guid.NewGuid(),
                EventId = saga.EventId.Value,
                LecturerId = saga.LecturerId ?? 0,
                DateTime = saga.LectureDateTime,
                DurationInHours = saga.LectureDurationInHours,
                ReplyTo = RoutingKeys.SagaReplyQueue
            };

            await _outboxService.AddAsync(
                sagaId: saga.Id,
                exchange: _exchanges.EventExchange,
                routingKey: RoutingKeys.SagaEventLectureCreateRequestQueue,
                message: command,
                ct: ct);

            saga.Log += BuildLogLine(
                $"CreateEventLectureSagaCommand added to outbox. EventId={command.EventId}, LecturerId={command.LecturerId}.");

            await _db.SaveChangesAsync(ct);
        }

        private async Task HandleEventLectureCreatedAsync(
            string json,
            CancellationToken ct)
        {
            var reply = JsonSerializer.Deserialize<EventLectureCreatedSagaReply>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (reply is null)
                throw new InvalidOperationException("EventLectureCreatedSagaReply nije validan.");

            var saga = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.Id == reply.SagaId, ct);

            if (saga is null)
            {
                _logger.LogWarning(
                    "Saga state not found for SagaId={SagaId}",
                    reply.SagaId);

                return;
            }

            if (!reply.Success || reply.EventLectureId is null)
            {
                saga.Status = SagaStatus.Compensating;
                saga.CurrentStep = "CompensateDeleteEvent";
                saga.ErrorMessage = reply.ErrorMessage ?? "Kreiranje predavanja nije uspelo.";
                saga.Log += BuildLogLine(
                    $"Event lecture creation failed. Compensation will delete EventId={saga.EventId}. Error={saga.ErrorMessage}");

                if (saga.EventId is not null)
                {
                    var deleteEventCommand = new DeleteEventSagaCommand
                    {
                        SagaId = saga.Id,
                        CorrelationId = Guid.NewGuid(),
                        EventId = saga.EventId.Value,
                        ReplyTo = RoutingKeys.SagaReplyQueue
                    };

                    await _outboxService.AddAsync(
                        sagaId: saga.Id,
                        exchange: _exchanges.EventExchange,
                        routingKey: RoutingKeys.SagaEventDeleteRequestQueue,
                        message: deleteEventCommand,
                        ct: ct);

                    saga.Log += BuildLogLine(
                        $"DeleteEventSagaCommand added to outbox. EventId={saga.EventId}.");
                }
                else
                {
                    saga.Status = SagaStatus.Failed;
                    saga.CurrentStep = "Failed";
                    saga.FailedAtUtc = DateTime.UtcNow;
                    saga.Log += BuildLogLine("Compensation cannot delete event because EventId is null.");
                }

                await _db.SaveChangesAsync(ct);

                return;
            }

            saga.EventLectureId = reply.EventLectureId.Value;
            saga.Status = SagaStatus.Completed;
            saga.CurrentStep = "Completed";
            saga.CompletedAtUtc = DateTime.UtcNow;
            saga.Log += BuildLogLine(
                $"Saga completed successfully. EventLectureId={saga.EventLectureId}.");

            await _db.SaveChangesAsync(ct);
        }

        private async Task HandleEventDeletedAsync(
            string json,
            CancellationToken ct)
        {
            var reply = JsonSerializer.Deserialize<EventDeletedSagaReply>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (reply is null)
                throw new InvalidOperationException("EventDeletedSagaReply nije validan.");

            var saga = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.Id == reply.SagaId, ct);

            if (saga is null)
            {
                _logger.LogWarning(
                    "Saga state not found for SagaId={SagaId}",
                    reply.SagaId);

                return;
            }

            if (!reply.Success)
            {
                saga.Status = SagaStatus.Failed;
                saga.CurrentStep = "CompensationFailed";
                saga.ErrorMessage = reply.ErrorMessage ?? "Kompenzacija nije uspela.";
                saga.FailedAtUtc = DateTime.UtcNow;
                saga.Log += BuildLogLine(
                    $"Event compensation failed. EventId={reply.EventId}. Error={saga.ErrorMessage}");

                await _db.SaveChangesAsync(ct);

                return;
            }

            saga.Status = SagaStatus.Compensated;
            saga.CurrentStep = "Compensated";
            saga.CompensatedAtUtc = DateTime.UtcNow;
            saga.Log += BuildLogLine(
                $"Compensation completed. EventId={reply.EventId} deleted.");

            await _db.SaveChangesAsync(ct);
        }

        private static string BuildLogLine(string message)
        {
            return $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}";
        }
    }
}
