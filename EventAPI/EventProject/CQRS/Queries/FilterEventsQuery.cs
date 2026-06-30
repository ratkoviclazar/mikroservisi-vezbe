using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.Queries.ReadModels;

namespace EventAPI.CQRS.Queries
{
    public sealed class FilterEventsQuery : IQuery<List<EventListItemReadModel>>
    {
        public string? NameContains { get; init; }
        public int? LocationId { get; init; }
        public int? TypeId { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
    }
}
