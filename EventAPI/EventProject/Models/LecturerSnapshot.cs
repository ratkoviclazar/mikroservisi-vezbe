namespace EventAPI.Domains
{
    public class LecturerSnapshot
    {
        public int Id { get; set; }

        public int ExternalId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string ExpertiseArea { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; }
    }
}