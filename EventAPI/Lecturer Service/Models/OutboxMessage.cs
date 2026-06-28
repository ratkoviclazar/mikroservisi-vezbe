namespace Lecturer_Service.Models
{
    public class OutboxMessage
    {
        public long Id { get; set; }

        public Guid MessageId { get; set; }

        public string Type { get; set; } = "";

        public string Payload { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAtUtc { get; set; }

        public bool IsProcessing { get; set; }

        public bool IsProcessed { get; set; }

    }
}
