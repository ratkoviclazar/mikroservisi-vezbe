namespace EventAPI.Domain.Events
{
    public sealed class EventAgendaChanged : DomainEvent
    {
        public string Agenda { get; set; } = string.Empty;
    }
}
