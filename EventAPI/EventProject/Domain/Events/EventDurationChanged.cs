namespace EventAPI.Domain.Events
{
    public sealed class EventDurationChanged : DomainEvent
    {
        public decimal DurationInHours { get; set; }
    }
}
