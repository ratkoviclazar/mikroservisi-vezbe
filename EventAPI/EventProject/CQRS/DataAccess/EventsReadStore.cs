using EventAPI.Data;
using EventAPI.Domains;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.CQRS.DataAccess
{
    public sealed class EventsReadStore : IEventsReadStore
    {
        private readonly EventsDbContext _context;

        public EventsReadStore(EventsDbContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetAllWithLecturesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Events
                .AsNoTracking()
                .Include(x => x.EventLectures)
                .ToListAsync(cancellationToken);
        }

        public async Task<Event?> GetByIdWithLecturesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Events
                .AsNoTracking()
                .Include(x => x.EventLectures)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<List<Event>> FilterAsync(
            string? nameContains,
            int? locationId,
            int? typeId,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Events
                .AsNoTracking()
                .Include(x => x.EventLectures)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(nameContains))
                query = query.Where(x => x.Name.Contains(nameContains));

            if (locationId.HasValue)
                query = query.Where(x => x.LocationId == locationId.Value);

            if (typeId.HasValue)
                query = query.Where(x => x.TypeId == typeId.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.DateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.DateTime <= toDate.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<LocationSnapshot?> GetLocationSnapshotAsync(int externalLocationId, CancellationToken cancellationToken = default)
        {
            return await _context.LocationSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExternalId == externalLocationId, cancellationToken);
        }

        public async Task<EventTypeSnapshot?> GetEventTypeSnapshotAsync(int externalTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.EventTypeSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExternalId == externalTypeId, cancellationToken);
        }
    }
}
