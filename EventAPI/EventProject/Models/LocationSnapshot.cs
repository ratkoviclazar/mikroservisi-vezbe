namespace EventAPI.Domains
{
    public class LocationSnapshot
    {
        public int Id { get; set; }

        public int ExternalId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }
}