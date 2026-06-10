namespace EventProject.EventService.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Agenda { get; set; }
        public DateTime DateTime { get; set; }
        public decimal DurationInHours { get; set; }
        public decimal Price { get; set; }
        public int TypeId { get; set; }
        public int LocationId { get; set; }
        public ICollection<EventLecture> EventLectures { get; set; } = new List<EventLecture>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
