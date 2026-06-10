using EventProject.CatalogService.Data;
using EventProject.CatalogService.Models;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventProject.CatalogService.Controllers;

[ApiController]
[Route("api/event-types")]
public class EventTypesController : ControllerBase
{
    private readonly ReferenceDbContext _context;

    public EventTypesController(ReferenceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventTypeDto>>> GetAll()
    {
        var result = await _context.EventTypes
            .Select(x => new EventTypeDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventTypeDto>> GetById(int id)
    {
        var result = await _context.EventTypes
            .Where(x => x.Id == id)
            .Select(x => new EventTypeDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstOrDefaultAsync();

        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EventTypeDto>> Create(EventTypeDto request)
    {
        var entity = new EventType
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EventTypes.Add(entity);
        await _context.SaveChangesAsync();

        request.Id = entity.Id;

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, request);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventTypeDto request)
    {
        var entity = await _context.EventTypes.FindAsync(id);

        if (entity == null)
            return NotFound();

        entity.Name = request.Name;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.EventTypes.FindAsync(id);

        if (entity == null)
            return NotFound();

        _context.EventTypes.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}