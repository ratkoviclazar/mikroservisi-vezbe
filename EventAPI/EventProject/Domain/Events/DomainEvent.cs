namespace EventAPI.Domain.Events
{
    public abstract class DomainEvent
    {
        public int AggregateId { get; set; }

        public int Version { get; set; }

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
