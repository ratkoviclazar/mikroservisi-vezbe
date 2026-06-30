namespace EventAPI.Domain.Events
{
    public sealed class EventTypeChanged : DomainEvent
    {
        public int TypeId { get; set; }
    }
}
