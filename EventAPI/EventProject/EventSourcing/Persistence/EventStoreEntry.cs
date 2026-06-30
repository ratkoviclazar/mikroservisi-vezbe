using System.ComponentModel.DataAnnotations;

namespace EventAPI.EventSourcing.Persistence
{
    public class EventStoreEntry
    {
        [Key]
        public long Id { get; set; }

        public int AggregateId { get; set; }

        public string AggregateType { get; set; } = "Event";

        public int Version { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string EventData { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
