using EventAPI.Data;
using EventAPI.Domains;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static int _counter = 0;
    private static int _timeoutCounter = 0;
    private readonly EventsDbContext _context;

    public EventsController(EventsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDetailsDto>>> GetAll()
    {
        _counter++;

        if (_counter % 10 != 0)
        {
            Console.WriteLine("Simulating server error for testing purposes.");
            return StatusCode(500, "Simulated server error");
        }

        var events = await _context.Events
            .Include(x => x.EventLectures)
            .ToListAsync();

        var result = new List<EventDetailsDto>();

        foreach (var ev in events)
        {
            var location = await _context.LocationSnapshots
                .FirstOrDefaultAsync(x => x.ExternalId == ev.LocationId);

            var eventType = await _context.EventTypeSnapshots
                .FirstOrDefaultAsync(x => x.ExternalId == ev.TypeId);

            result.Add(new EventDetailsDto
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
                    : new LocationDto
                    {
                        Id = location.ExternalId,
                        Name = location.Name,
                        Address = location.Address,
                        Capacity = location.Capacity
                    },

                EventType = eventType == null
                    ? null
                    : new EventTypeDto
                    {
                        Id = eventType.ExternalId,
                        Name = eventType.Name
                    },

                EventLectures = ev.EventLectures.Select(x => new EventLectureDto
                {
                    Id = x.Id,
                    EventId = x.EventId,
                    LecturerId = x.LecturerId,
                    DateTime = x.DateTime,
                    DurationInHours = x.DurationInHours
                }).ToList()
            });
        }

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDetailsDto>> GetById(int id)
    {
        _timeoutCounter++;
        if (_timeoutCounter % 2 == 0)
        {
            Console.WriteLine("Simulating timeout for testing purposes.");
            await Task.Delay(7000);
        }
        var ev = await _context.Events
            .Include(x => x.EventLectures)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ev == null)
            return NotFound();

        var location = await _context.LocationSnapshots
            .FirstOrDefaultAsync(x => x.ExternalId == ev.LocationId);

        var eventType = await _context.EventTypeSnapshots
            .FirstOrDefaultAsync(x => x.ExternalId == ev.TypeId);

        var result = new EventDetailsDto
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
                : new LocationDto
                {
                    Id = location.ExternalId,
                    Name = location.Name,
                    Address = location.Address,
                    Capacity = location.Capacity
                },

            EventType = eventType == null
                ? null
                : new EventTypeDto
                {
                    Id = eventType.ExternalId,
                    Name = eventType.Name
                },

            EventLectures = ev.EventLectures.Select(x => new EventLectureDto
            {
                Id = x.Id,
                EventId = x.EventId,
                LecturerId = x.LecturerId,
                DateTime = x.DateTime,
                DurationInHours = x.DurationInHours
            }).ToList()
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EventDetailsDto>> Create(CreateEventDto dto)
    {
        if (dto.DurationInHours <= 0)
            ModelState.AddModelError(nameof(dto.DurationInHours), "Duration must be greater than 0.");

        if (dto.Price < 0)
            ModelState.AddModelError(nameof(dto.Price), "Price cannot be negative.");

        var locationExists = await _context.LocationSnapshots
            .AnyAsync(x => x.ExternalId == dto.LocationId);

        if (!locationExists)
            ModelState.AddModelError(nameof(dto.LocationId), "Location does not exist.");

        var eventTypeExists = await _context.EventTypeSnapshots
            .AnyAsync(x => x.ExternalId == dto.TypeId);

        if (!eventTypeExists)
            ModelState.AddModelError(nameof(dto.TypeId), "Event type does not exist.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var entity = new Event
        {
            Name = dto.Name,
            Agenda = dto.Agenda,
            DateTime = dto.DateTime,
            DurationInHours = dto.DurationInHours,
            Price = dto.Price,
            TypeId = dto.TypeId,
            LocationId = dto.LocationId
        };

        _context.Events.Add(entity);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEventDto dto)
    {
        var entity = await _context.Events.FindAsync(id);

        if (entity == null)
            return NotFound();

        entity.Name = dto.Name;
        entity.Agenda = dto.Agenda;
        entity.DateTime = dto.DateTime;
        entity.DurationInHours = dto.DurationInHours;
        entity.Price = dto.Price;
        entity.TypeId = dto.TypeId;
        entity.LocationId = dto.LocationId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Events.FindAsync(id);

        if (entity == null)
            return NotFound();

        _context.Events.Remove(entity);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}