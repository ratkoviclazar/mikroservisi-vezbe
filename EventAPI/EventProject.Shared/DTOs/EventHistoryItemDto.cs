namespace EventAPI.DTO.DTOs
{
    public sealed class EventHistoryItemDto
    {
        public int Version { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string EventData { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }
    }
}
