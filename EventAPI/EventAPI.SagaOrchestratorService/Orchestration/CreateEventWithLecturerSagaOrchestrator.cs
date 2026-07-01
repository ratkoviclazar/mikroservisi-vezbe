using EventAPI.DTO.Messaging.Saga;
using EventAPI.DTO.Shared;
using EventAPI.SagaOrchestratorService.Data;
using EventAPI.SagaOrchestratorService.DTOs;
using EventAPI.SagaOrchestratorService.Messaging;
using EventAPI.SagaOrchestratorService.Models;
using EventAPI.SagaOrchestratorService.Options;
using Microsoft.Extensions.Options;

namespace EventAPI.SagaOrchestratorService.Orchestration
{
    public class CreateEventWithLecturerSagaOrchestrator : ICreateEventWithLecturerSagaOrchestrator
    {
        private readonly SagaDbContext _db;
        private readonly ISagaOutboxService _outboxService;
        private readonly ILogger<CreateEventWithLecturerSagaOrchestrator> _logger;
        private readonly ServiceExchangesOptions _exchanges;
        public CreateEventWithLecturerSagaOrchestrator(
            SagaDbContext db,
            ISagaOutboxService outboxService,
            ILogger<CreateEventWithLecturerSagaOrchestrator> logger,
            IOptions<ServiceExchangesOptions> exchanges)
        {
            _db = db;
            _outboxService = outboxService;
            _logger = logger;
            _exchanges = exchanges.Value;
        }

        public async Task<CreateEventWithLecturerSagaResponse> StartAsync(
            CreateEventWithLecturerSagaRequest request,
            CancellationToken ct = default)
        {
            var sagaId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            var saga = new SagaState
            {
                Id = sagaId,
                SagaType = "CreateEventWithLecturerSaga",
                Status = SagaStatus.Started,
                CurrentStep = "ValidateReferenceData",

                LocationId = request.LocationId,
                TypeId = request.TypeId,
                LecturerId = request.LecturerId,

                EventName = request.Name,
                EventAgenda = request.Agenda,
                EventDateTime = request.DateTime,
                EventDurationInHours = request.DurationInHours,
                EventPrice = request.Price,

                LectureDateTime = request.LectureDateTime,
                LectureDurationInHours = request.LectureDurationInHours,

                SimulateLectureCreationFailure = request.SimulateLectureCreationFailure,
                StartedAtUtc = DateTime.UtcNow,
                Log = BuildLogLine("Saga started.")
            };

            _db.SagaStates.Add(saga);

            var command = new ValidateReferenceDataCommand
            {
                SagaId = sagaId,
                CorrelationId = correlationId,
                LocationId = request.LocationId,
                EventTypeId = request.TypeId,
                ReplyTo = RoutingKeys.SagaReplyQueue
            };

            await _outboxService.AddAsync(
                sagaId: sagaId,
                exchange: _exchanges.ReferenceExchange,
                routingKey: RoutingKeys.SagaReferenceValidateRequestQueue,
                message: command,
                ct: ct);

            saga.Log += BuildLogLine(
                $"ValidateReferenceDataCommand added to outbox. LocationId={request.LocationId}, EventTypeId={request.TypeId}.");

            await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Saga {SagaId} started. First command added to outbox.",
                sagaId);

            return new CreateEventWithLecturerSagaResponse
            {
                SagaId = sagaId,
                Status = saga.Status.ToString(),
                EventId = null,
                EventLectureId = null,
                Message = "Saga started. Reference data validation command was added to outbox."
            };
        }

        private static string BuildLogLine(string message)
        {
            return $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}";
        }
    }
}
