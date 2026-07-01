namespace EventAPI.DTO.Messaging.Saga
{
    public abstract class SagaMessage
    {
        public Guid SagaId { get; set; }

        public Guid CorrelationId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
