using EventAPI.Data;
using EventAPI.Domains;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAPI.Controllers;

[ApiController]
[Route("api/event-lectures")]
public class EventLecturesController : ControllerBase
{
    private readonly EventsDbContext _context;

    public EventLecturesController(EventsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventLectureDto>>> GetAll()
    {
        var lectures = await _context.EventLectures
            .OrderBy(x => x.DateTime)
            .Select(x => new EventLectureDto
            {
                Id = x.Id,
                DateTime = x.DateTime,
                DurationInHours = x.DurationInHours,
                EventId = x.EventId,
                LecturerId = x.LecturerId
            })
            .ToListAsync();

        return Ok(lectures);
    }

    [HttpGet("by-event/{id:int}")]
    public async Task<ActionResult<EventLectureDto>> GetByEventId(int id)
    {
        var eventData = await _context.Events.
        Where(x => x.Id == id).Select(x => new EventDetailsDto
        {
            Id = x.Id,
            Name = x.Name,
            Agenda = x.Agenda
        }).FirstOrDefaultAsync();

        var lectures = await _context.EventLectures
        .Where(x => x.EventId == id)
        .OrderBy(x => x.DateTime)
        .Select(x => new EventLectureDto
        {
            Id = x.Id,
            DateTime = x.DateTime,
            DurationInHours = x.DurationInHours,
            EventId = x.EventId,
            Event = eventData == null ? null : new EventDetailsDto
            {
                Id = eventData.Id,
                Name = eventData.Name,
                Agenda = eventData.Agenda
            },
            LecturerId = x.LecturerId
        })
        .ToListAsync();

        if (lectures == null)
            return NotFound();

        return Ok(lectures);
    }

    [HttpPost]
    public async Task<ActionResult<EventLectureDto>> Create(CreateEventLectureDto dto)
    {
        if (dto.DurationInHours <= 0)
            return ValidationProblem("Duration must be greater than 0.");

        var eventExists = await _context.Events.AnyAsync(x => x.Id == dto.EventId);
        if (!eventExists)
            return ValidationProblem("Event does not exist.");

        var entity = new EventLecture
        {
            DateTime = dto.DateTime,
            DurationInHours = dto.DurationInHours,
            EventId = dto.EventId,
            LecturerId = dto.LecturerId
        };

        _context.EventLectures.Add(entity);
        await _context.SaveChangesAsync();

        var result = new EventLectureDto
        {
            Id = entity.Id,
            DateTime = entity.DateTime,
            DurationInHours = entity.DurationInHours,
            EventId = entity.EventId,
            LecturerId = entity.LecturerId
        };

        return CreatedAtAction(nameof(GetByEventId), new { id = entity.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEventLectureDto dto)
    {
        if (dto.DurationInHours <= 0)
            return ValidationProblem("Duration must be greater than 0.");

        var entity = await _context.EventLectures.FindAsync(id);

        if (entity == null)
            return NotFound();

        entity.DateTime = dto.DateTime;
        entity.DurationInHours = dto.DurationInHours;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.EventLectures.FindAsync(id);

        if (entity == null)
            return NotFound();

        _context.EventLectures.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}