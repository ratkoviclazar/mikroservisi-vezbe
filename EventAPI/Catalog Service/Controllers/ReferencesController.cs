using EventProject.CatalogService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventProject.CatalogService.Controllers;

[ApiController]
[Route("api/references")]
public class ReferenceController : ControllerBase
{
    private readonly ReferenceDbContext _context;

    public ReferenceController(ReferenceDbContext context)
    {
        _context = context;
    }

    [HttpGet("validate")]
    public async Task<ActionResult<object>> Validate([FromQuery] int locationId, [FromQuery] int eventTypeId)
    {
        var locationExists = await _context.Locations
            .AnyAsync(x => x.Id == locationId);

        var eventTypeExists = await _context.EventTypes
            .AnyAsync(x => x.Id == eventTypeId);

        var isValid = locationExists && eventTypeExists;

        return Ok(new
        {
            IsValid = isValid,
            LocationExists = locationExists,
            EventTypeExists = eventTypeExists
        });
    }
}