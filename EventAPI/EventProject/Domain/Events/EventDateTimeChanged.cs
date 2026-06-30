namespace EventAPI.Domain.Events
{
    public sealed class EventDateTimeChanged : DomainEvent
    {
        public DateTime DateTime { get; set; }
    }
}
