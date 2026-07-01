namespace EventAPI.SagaOrchestratorService.Messaging
{
    public interface ISagaReplyHandler
    {
        Task HandleAsync(
            string messageType,
            string json,
            CancellationToken ct = default);
    }
}
