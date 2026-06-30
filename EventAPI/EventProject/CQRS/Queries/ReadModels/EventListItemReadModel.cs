namespace EventAPI.CQRS.Queries.ReadModels
{
    public sealed class EventListItemReadModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Agenda { get; init; } = string.Empty;
        public DateTime DateTime { get; init; }
        public decimal DurationInHours { get; init; }
        public decimal Price { get; init; }
        public int TypeId { get; init; }
        public int LocationId { get; init; }
        public string? LocationName { get; init; }
        public string? EventTypeName { get; init; }
        public int LectureCount { get; init; }
    }
}
