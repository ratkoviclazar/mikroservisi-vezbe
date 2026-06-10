namespace EventAPI.Domains
{
    public class EventTypeSnapshot
    {
        public int Id { get; set; }

        public int ExternalId { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; }
    }
}