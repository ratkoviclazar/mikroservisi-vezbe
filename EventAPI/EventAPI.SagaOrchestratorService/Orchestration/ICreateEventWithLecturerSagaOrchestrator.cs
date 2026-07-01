using EventAPI.SagaOrchestratorService.DTOs;

namespace EventAPI.SagaOrchestratorService.Orchestration
{
    public interface ICreateEventWithLecturerSagaOrchestrator
    {
        Task<CreateEventWithLecturerSagaResponse> StartAsync(
            CreateEventWithLecturerSagaRequest request,
            CancellationToken ct = default);
    }
}
