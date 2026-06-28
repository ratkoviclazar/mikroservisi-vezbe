using Catalog_Service.Models;
using EventProject.CatalogService.Data;
using EventProject.CatalogService.Models;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventProject.CatalogService.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ReferenceDbContext _context;

    public LocationsController(ReferenceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll()
    {
        var result = await _context.Locations
            .Select(x => new LocationDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                Capacity = x.Capacity
            })
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id)
    {
        var result = await _context.Locations
            .Where(x => x.Id == id)
            .Select(x => new LocationDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                Capacity = x.Capacity
            })
            .FirstOrDefaultAsync();
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LocationDto>> Create(LocationDto request)
    {
        var entity = new Location
        {
            Name = request.Name,
            Address = request.Address,
            Capacity = request.Capacity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Locations.Add(entity);
        await _context.SaveChangesAsync();

        _context.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = "location.created",
            Payload = JsonSerializer.Serialize(new LocationDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Address = entity.Address,
                Capacity = entity.Capacity
            }),
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
    public async Task<IActionResult> Update(int id, LocationDto request)
    {
        var entity = await _context.Locations.FindAsync(id);
        if (entity == null)
            return NotFound();

        entity.Name = request.Name;
        entity.Address = request.Address;
        entity.Capacity = request.Capacity;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = "location.updated",
            Payload = JsonSerializer.Serialize(new LocationDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Address = entity.Address,
                Capacity = entity.Capacity
            }),
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
        var entity = await _context.Locations.FindAsync(id);
        if (entity == null)
            return NotFound();

        var payload = JsonSerializer.Serialize(new LocationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Address = entity.Address,
            Capacity = entity.Capacity
        });

        _context.Locations.Remove(entity);

        _context.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = "location.deleted",
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false,
            IsProcessing = false
        });

        await _context.SaveChangesAsync();
        return NoContent();
    }
}