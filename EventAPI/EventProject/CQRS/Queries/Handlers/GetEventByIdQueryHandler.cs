using EventAPI.CQRS.Abstractions;
using EventAPI.CQRS.DataAccess;
using EventAPI.CQRS.Queries.ReadModels;

namespace EventAPI.CQRS.Queries.Handlers
{
    public sealed class GetEventByIdQueryHandler : IQueryHandler<GetEventByIdQuery, EventDetailsReadModel?>
    {
        private readonly IEventsReadStore _readStore;

        public GetEventByIdQueryHandler(IEventsReadStore readStore)
        {
            _readStore = readStore;
        }

        public async Task<EventDetailsReadModel?> HandleAsync(GetEventByIdQuery query, CancellationToken cancellationToken = default)
        {
            var ev = await _readStore.GetByIdWithLecturesAsync(query.Id, cancellationToken);

            if (ev == null)
                return null;

            var location = await _readStore.GetLocationSnapshotAsync(ev.LocationId, cancellationToken);
            var eventType = await _readStore.GetEventTypeSnapshotAsync(ev.TypeId, cancellationToken);

            return new EventDetailsReadModel
            {
                Id = ev.Id,
                Name = ev.Name,
                Agenda = ev.Agenda,
                DateTime = ev.DateTime,
                DurationInHours = ev.DurationInHours,
                Price = ev.Price,
                TypeId = ev.TypeId,
                LocationId = ev.LocationId,
                Location = location == null
                    ? null
                    : new LocationReadModel
                    {
                        Id = location.ExternalId,
                        Name = location.Name,
                        Address = location.Address,
                        Capacity = location.Capacity
                    },
                EventType = eventType == null
                    ? null
                    : new EventTypeReadModel
                    {
                        Id = eventType.ExternalId,
                        Name = eventType.Name
                    },
                Lectures = ev.EventLectures.Select(l => new EventLectureReadModel
                {
                    Id = l.Id,
                    DateTime = l.DateTime,
                    DurationInHours = l.DurationInHours,
                    LecturerId = l.LecturerId
                }).ToList()
            };
        }
    }
}
