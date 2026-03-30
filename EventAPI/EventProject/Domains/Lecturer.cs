namespace EventAPI.Domains
{
    public class Lecturer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Title { get; set; }
        public string ExpertiseArea { get; set; }
        
        public ICollection<EventLecture> EventLectures = new List<EventLecture>();
    }
}
