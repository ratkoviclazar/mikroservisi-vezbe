namespace EventAPI.CQRS.Queries.ReadModels
{
    public sealed class EventDetailsReadModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Agenda { get; init; } = string.Empty;
        public DateTime DateTime { get; init; }
        public decimal DurationInHours { get; init; }
        public decimal Price { get; init; }
        public int TypeId { get; init; }
        public int LocationId { get; init; }
        public LocationReadModel? Location { get; init; }
        public EventTypeReadModel? EventType { get; init; }
        public List<EventLectureReadModel> Lectures { get; init; } = new();
    }

    public sealed class LocationReadModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public int Capacity { get; init; }
    }

    public sealed class EventTypeReadModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public sealed class EventLectureReadModel
    {
        public int Id { get; init; }
        public DateTime DateTime { get; init; }
        public decimal DurationInHours { get; init; }
        public int LecturerId { get; init; }
    }
}
