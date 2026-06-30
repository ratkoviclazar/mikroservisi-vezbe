namespace EventAPI.Domain.Events
{
    public sealed class EventNameChanged : DomainEvent
    {
        public string Name { get; set; } = string.Empty;
    }
}
