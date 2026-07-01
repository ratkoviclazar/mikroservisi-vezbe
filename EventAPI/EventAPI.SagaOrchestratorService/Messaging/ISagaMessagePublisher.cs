namespace EventAPI.SagaOrchestratorService.Messaging
{
    public interface ISagaMessagePublisher
    {
        Task PublishAsync<TMessage>(
            TMessage message,
            string exchange,
            string routingKey,
            CancellationToken ct = default)
            where TMessage : class;

        Task PublishRawAsync(
            string exchange,
            string payload,
            string messageType,
            string routingKey,
            CancellationToken ct = default);
    }
}
