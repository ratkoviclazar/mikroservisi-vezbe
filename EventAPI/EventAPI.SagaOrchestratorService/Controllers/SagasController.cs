using EventAPI.SagaOrchestratorService.Data;
using EventAPI.SagaOrchestratorService.DTOs;
using EventAPI.SagaOrchestratorService.Orchestration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.SagaOrchestratorService.Controllers
{
    [ApiController]
    [Route("api/sagas")]
    public class SagasController : ControllerBase
    {
        private readonly ICreateEventWithLecturerSagaOrchestrator _orchestrator;
        private readonly SagaDbContext _db;

        public SagasController(
            ICreateEventWithLecturerSagaOrchestrator orchestrator,
            SagaDbContext db)
        {
            _orchestrator = orchestrator;
            _db = db;
        }

        [HttpPost("create-event-with-lecturer")]
        public async Task<ActionResult<CreateEventWithLecturerSagaResponse>> CreateEventWithLecturer(
            CreateEventWithLecturerSagaRequest request,
            CancellationToken ct)
        {
            var result = await _orchestrator.StartAsync(request, ct);

            return AcceptedAtAction(
                nameof(GetById),
                new { id = result.SagaId },
                result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SagaStateDto>> GetById(Guid id, CancellationToken ct)
        {
            var saga = await _db.SagaStates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (saga is null)
                return NotFound();

            return Ok(new SagaStateDto
            {
                Id = saga.Id,
                SagaType = saga.SagaType,
                Status = saga.Status.ToString(),
                CurrentStep = saga.CurrentStep,
                EventId = saga.EventId,
                EventLectureId = saga.EventLectureId,
                LocationId = saga.LocationId,
                TypeId = saga.TypeId,
                LecturerId = saga.LecturerId,
                ErrorMessage = saga.ErrorMessage,
                Log = saga.Log,
                StartedAtUtc = saga.StartedAtUtc,
                CompletedAtUtc = saga.CompletedAtUtc,
                FailedAtUtc = saga.FailedAtUtc,
                CompensatedAtUtc = saga.CompensatedAtUtc
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<SagaStateDto>>> GetAll(CancellationToken ct)
        {
            var sagas = await _db.SagaStates
                .AsNoTracking()
                .OrderByDescending(x => x.StartedAtUtc)
                .Take(50)
                .Select(saga => new SagaStateDto
                {
                    Id = saga.Id,
                    SagaType = saga.SagaType,
                    Status = saga.Status.ToString(),
                    CurrentStep = saga.CurrentStep,
                    EventId = saga.EventId,
                    EventLectureId = saga.EventLectureId,
                    LocationId = saga.LocationId,
                    TypeId = saga.TypeId,
                    LecturerId = saga.LecturerId,
                    ErrorMessage = saga.ErrorMessage,
                    Log = saga.Log,
                    StartedAtUtc = saga.StartedAtUtc,
                    CompletedAtUtc = saga.CompletedAtUtc,
                    FailedAtUtc = saga.FailedAtUtc,
                    CompensatedAtUtc = saga.CompensatedAtUtc
                })
                .ToListAsync(ct);

            return Ok(sagas);
        }
    }
}
