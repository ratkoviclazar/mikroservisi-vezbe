namespace EventProject.LecturerService.Models
{
    public class LecturerDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExpertiseArea { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}