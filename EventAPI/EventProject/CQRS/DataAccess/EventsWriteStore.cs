using EventAPI.Data;
using EventAPI.Domains;
using EventAPI.DTO.Messaging;
using EventAPI.DTO.Shared;
using EventAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventAPI.CQRS.DataAccess
{
    public sealed class EventsWriteStore : IEventsWriteStore
    {
        private readonly EventsDbContext _context;

        public EventsWriteStore(EventsDbContext context)
        {
            _context = context;
        }

        public Task<bool> LocationExistsAsync(int locationId, CancellationToken cancellationToken = default)
        {
            return _context.LocationSnapshots.AnyAsync(x => x.ExternalId == locationId, cancellationToken);
        }

        public Task<bool> EventTypeExistsAsync(int typeId, CancellationToken cancellationToken = default)
        {
            return _context.EventTypeSnapshots.AnyAsync(x => x.ExternalId == typeId, cancellationToken);
        }

        public async Task<int> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default)
        {
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync(cancellationToken);

            var locationSnapshot = await _context.LocationSnapshots
                .FirstOrDefaultAsync(x => x.ExternalId == newEvent.LocationId, cancellationToken);

            var locationName = locationSnapshot?.Name ?? "Nepoznata lokacija";

            var emailMessage = new EmailMessage
            {
                Id = Guid.NewGuid(),
                To = "org@example.com",
                Subject = $"Kreiran novi događaj: {newEvent.Name}",
                Body = $"Događaj {newEvent.Name} dana {newEvent.DateTime:dd.MM.yyyy HH:mm} na lokaciji {locationName}",
                EnqueuedAt = DateTime.UtcNow
            };

            var emailPayload = JsonSerializer.Serialize(emailMessage);

            _context.OutboxMessages.Add(new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Type = RoutingKeys.EmailSent,
                Payload = emailPayload,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false,
                IsProcessing = false
            });

            await _context.SaveChangesAsync(cancellationToken);

            return newEvent.Id;
        }

        public async Task<bool> UpdateEventAsync(int id, Action<Event> applyChanges, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Events.FindAsync(new object?[] { id }, cancellationToken);

            if (entity == null)
                return false;

            applyChanges(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteEventAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Events.FindAsync(new object?[] { id }, cancellationToken);

            if (entity == null)
                return false;

            _context.Events.Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
