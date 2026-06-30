namespace EventAPI.Domain.Events
{
    public sealed class EventLocationChanged : DomainEvent
    {
        public int LocationId { get; set; }
    }
}
