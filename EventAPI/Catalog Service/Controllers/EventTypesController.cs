using Catalog_Service.Models;
using EventProject.CatalogService.Data;
using EventProject.CatalogService.Models;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            .Select(x => new EventTypeDto { Id = x.Id, Name = x.Name })
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventTypeDto>> GetById(int id)
    {
        var result = await _context.EventTypes
            .Where(x => x.Id == id)
            .Select(x => new EventTypeDto { Id = x.Id, Name = x.Name })
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


        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.EventTypes.Add(entity);
        await _context.SaveChangesAsync();

        _context.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = "eventtype.created",
            Payload = JsonSerializer.Serialize(new EventTypeDto { Id = entity.Id, Name = entity.Name }),
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false,
            IsProcessing = false
        });
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

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

        _context.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = "eventtype.updated",
            Payload = JsonSerializer.Serialize(new EventTypeDto { Id = entity.Id, Name = entity.Name }),
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false,
            IsProcessing = false
        });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.EventTypes.FindAsync(id);
        if (entity == null)
            return NotFound();

        var payload = JsonSerializer.Serialize(new EventTypeDto { Id = entity.Id, Name = entity.Name });

        _context.EventTypes.Remove(entity);

        _context.OutboxMessages.Add(new Catalog_Service.Models.OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = "eventtype.deleted",
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false,
            IsProcessing = false
        });

        await _context.SaveChangesAsync();
        return NoContent();
    }
}