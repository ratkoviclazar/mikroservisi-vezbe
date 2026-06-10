using EventProject.LecturerService.Data;
using EventProject.LecturerService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventProject.LecturerService.Controllers;

[ApiController]
[Route("api/lecturers")]
public class LecturerController : ControllerBase
{
    private readonly LecturerDbContext _context;

    public LecturerController(LecturerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LecturerDto>>> GetAll()
    {
        var result = await _context.Lecturers
            .OrderBy(x => x.Name)
            .Select(x => new LecturerDto
            {
                Id = x.Id,
                Name = x.Name,
                Surname = x.Surname,
                Title = x.Title,
                ExpertiseArea = x.ExpertiseArea
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LecturerDto>> GetById(int id)
    {
        var result = await _context.Lecturers
            .Where(x => x.Id == id)
            .Select(x => new LecturerDto
            {
                Id = x.Id,
                Name = x.Name,
                Surname = x.Surname,
                Title = x.Title,
                ExpertiseArea = x.ExpertiseArea
            })
            .FirstOrDefaultAsync();

        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LecturerDto>> Create(LecturerDto dto)
    {
        var entity = new Lecturer
        {
            Name = dto.Name,
            Surname = dto.Surname,
            Title = dto.Title,
            ExpertiseArea = dto.ExpertiseArea,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Lecturers.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LecturerDto dto)
    {
        var entity = await _context.Lecturers.FindAsync(id);

        if (entity == null)
            return NotFound();

        entity.Name = dto.Name;
        entity.Surname = dto.Surname;
        entity.Title = dto.Title;
        entity.ExpertiseArea = dto.ExpertiseArea;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Lecturers.FindAsync(id);

        if (entity == null)
            return NotFound();

        _context.Lecturers.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}