using System.ComponentModel.DataAnnotations;

namespace EventAPI.EventSourcing.Persistence
{
    public class EventSnapshot
    {
        [Key]
        public long Id { get; set; }

        public int AggregateId { get; set; }

        public int Version { get; set; }

        public string State { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
