using EventAPI.Data;
using EventAPI.Domains;
using EventAPI.DTO.Shared;
using EventAPI.Models;
using EventProject.DTO.DTOs;
using EventProject.LecturerService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventAPI.Messaging
{
    public class MessageDispatcher
    {
        private readonly EventsDbContext _db;
        private readonly ILogger<MessageDispatcher> _logger;

        public MessageDispatcher(EventsDbContext db, ILogger<MessageDispatcher> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task HandleAsync(Guid messageId, string type, string json, CancellationToken cancellationToken)
        {
            var eventIdString = messageId.ToString();

            var alreadyProcessed = await _db.ProcessedMessages
                .AnyAsync(m => m.EventId == eventIdString, cancellationToken);

            if (alreadyProcessed)
            {
                _logger.LogDebug("Poruka {MessageId} je već obrađena, preskačem.", messageId);
                return;
            }


            switch (type)
            {
                case RoutingKeys.LocationCreated:
                case RoutingKeys.LocationUpdated:
                    await UpsertLocationAsync(json, cancellationToken);
                    break;

                case RoutingKeys.LocationDeleted:
                    await DeleteLocationAsync(json, cancellationToken);
                    break;

                case RoutingKeys.EventTypeCreated:
                case RoutingKeys.EventTypeUpdated:
                    await UpsertEventTypeAsync(json, cancellationToken);
                    break;

                case RoutingKeys.EventTypeDeleted:
                    await DeleteEventTypeAsync(json, cancellationToken);
                    break;

                case RoutingKeys.LecturerCreated:
                case RoutingKeys.LecturerUpdated:
                    await UpsertLecturerAsync(json, cancellationToken);
                    break;

                case RoutingKeys.LecturerDeleted:
                    await DeleteLecturerAsync(json, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Nepoznat tip poruke: {Type}, MessageId: {MessageId}", type, messageId);
                    return;
            }

            _db.ProcessedMessages.Add(new ProcessedMessage
            {
                EventId = eventIdString,
                EventType = type,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpsertLocationAsync(string json, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<LocationDto>(json)!;
            var existing = await _db.LocationSnapshots.FirstOrDefaultAsync(x => x.ExternalId == payload.Id, ct);

            if (existing is null)
                _db.LocationSnapshots.Add(new LocationSnapshot
                {
                    ExternalId = payload.Id,
                    Name = payload.Name,
                    Address = payload.Address,
                    Capacity = payload.Capacity,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            else
            {
                existing.Name = payload.Name;
                existing.Address = payload.Address;
                existing.Capacity = payload.Capacity;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        private async Task DeleteLocationAsync(string json, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<LocationDto>(json)!;
            var existing = await _db.LocationSnapshots
                   .FirstOrDefaultAsync(x => x.ExternalId == payload.Id, ct);
            if (existing is not null) _db.LocationSnapshots.Remove(existing);
        }

        private async Task UpsertEventTypeAsync(string json, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<EventTypeDto>(json)!;
            var existing = await _db.EventTypeSnapshots.FirstOrDefaultAsync(x => x.ExternalId == payload.Id, ct);

            if (existing is null)
                _db.EventTypeSnapshots.Add(new EventTypeSnapshot
                {
                    ExternalId = payload.Id,
                    Name = payload.Name,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            else
            {
                existing.Name = payload.Name;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        private async Task DeleteEventTypeAsync(string json, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<EventTypeDto>(json)!;
            var existing = await _db.EventTypeSnapshots.FirstOrDefaultAsync(x => x.ExternalId == payload.Id, ct);
            if (existing is not null) _db.EventTypeSnapshots.Remove(existing);
        }

        private async Task UpsertLecturerAsync(string json, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<LecturerDto>(json)!;
            var existing = await _db.LecturerSnapshots.FirstOrDefaultAsync(x => x.ExternalId == payload.Id, ct);

            if (existing is null)
                _db.LecturerSnapshots.Add(new LecturerSnapshot
                {
                    ExternalId = payload.Id,
                    Name = payload.Name,
                    Surname = payload.Surname,
                    Title = payload.Title,
                    ExpertiseArea = payload.ExpertiseArea,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            else
            {
                existing.Name = payload.Name;
                existing.Surname = payload.Surname;
                existing.Title = payload.Title;
                existing.ExpertiseArea = payload.ExpertiseArea;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        private async Task DeleteLecturerAsync(string json, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<LecturerDto>(json)!;
            var existing = await _db.LecturerSnapshots.FirstOrDefaultAsync(x => x.ExternalId == payload.Id, ct);
            if (existing is not null) _db.LecturerSnapshots.Remove(existing);

            var orphans = await _db.EventLectures.Where(el => el.LecturerId == payload.Id).ToListAsync(ct);
            _db.EventLectures.RemoveRange(orphans);
        }
    }
}
