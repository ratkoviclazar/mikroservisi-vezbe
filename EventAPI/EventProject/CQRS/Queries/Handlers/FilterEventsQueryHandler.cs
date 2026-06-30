using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.DataAccess;
using EventAPI.CQRS.Queries.ReadModels;

namespace EventAPI.CQRS.Queries.Handlers
{

    public sealed class FilterEventsQueryHandler : IQueryHandler<FilterEventsQuery, List<EventListItemReadModel>>
    {
        private readonly IEventsReadStore _readStore;

        public FilterEventsQueryHandler(IEventsReadStore readStore)
        {
            _readStore = readStore;
        }

        public async Task<List<EventListItemReadModel>> HandleAsync(FilterEventsQuery query, CancellationToken cancellationToken = default)
        {
            var events = await _readStore.FilterAsync(
                query.NameContains,
                query.LocationId,
                query.TypeId,
                query.FromDate,
                query.ToDate,
                cancellationToken);

            var result = new List<EventListItemReadModel>();

            foreach (var ev in events)
            {
                var location = await _readStore.GetLocationSnapshotAsync(ev.LocationId, cancellationToken);
                var eventType = await _readStore.GetEventTypeSnapshotAsync(ev.TypeId, cancellationToken);

                result.Add(new EventListItemReadModel
                {
                    Id = ev.Id,
                    Name = ev.Name,
                    Agenda = ev.Agenda,
                    DateTime = ev.DateTime,
                    DurationInHours = ev.DurationInHours,
                    Price = ev.Price,
                    TypeId = ev.TypeId,
                    LocationId = ev.LocationId,
                    LocationName = location?.Name,
                    EventTypeName = eventType?.Name,
                    LectureCount = ev.EventLectures.Count
                });
            }

            return result;
        }
    }
}
