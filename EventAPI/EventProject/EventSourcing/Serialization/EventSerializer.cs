using EventAPI.Domain.Events;
using System.Text.Json;

namespace EventAPI.EventSourcing.Serialization
{
    public static class EventSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static string Serialize(DomainEvent domainEvent)
        {
            return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Options);
        }

        public static DomainEvent Deserialize(string eventType, string eventData)
        {
            return eventType switch
            {
                nameof(EventCreated) =>
                    JsonSerializer.Deserialize<EventCreated>(eventData, Options)!,

                nameof(EventNameChanged) =>
                    JsonSerializer.Deserialize<EventNameChanged>(eventData, Options)!,

                nameof(EventAgendaChanged) =>
                    JsonSerializer.Deserialize<EventAgendaChanged>(eventData, Options)!,

                nameof(EventDateTimeChanged) =>
                    JsonSerializer.Deserialize<EventDateTimeChanged>(eventData, Options)!,

                nameof(EventDurationChanged) =>
                    JsonSerializer.Deserialize<EventDurationChanged>(eventData, Options)!,

                nameof(EventPriceChanged) =>
                    JsonSerializer.Deserialize<EventPriceChanged>(eventData, Options)!,

                nameof(EventTypeChanged) =>
                    JsonSerializer.Deserialize<EventTypeChanged>(eventData, Options)!,

                nameof(EventLocationChanged) =>
                    JsonSerializer.Deserialize<EventLocationChanged>(eventData, Options)!,

                nameof(EventDeleted) =>
                    JsonSerializer.Deserialize<EventDeleted>(eventData, Options)!,

                _ => throw new InvalidOperationException($"Nepoznat event tip: {eventType}")
            };
        }
    }
}
