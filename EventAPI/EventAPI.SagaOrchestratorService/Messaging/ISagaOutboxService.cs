namespace EventAPI.SagaOrchestratorService.Messaging
{
    public interface ISagaOutboxService
    {
        Task AddAsync<TMessage>(
            Guid sagaId,
            string exchange,
            string routingKey,
            TMessage message,
            CancellationToken ct = default)
            where TMessage : class;
    }
}