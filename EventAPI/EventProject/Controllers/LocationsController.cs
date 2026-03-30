using EventAPI.Data;
using EventAPI.Domains;
using Microsoft.AspNetCore.Mvc;

public class LocationsController : Controller
{
    private readonly EventDbContext _context;

    public LocationsController(EventDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var locations = _context.Locations
            .Select(l => new LocationViewModel
            {
                Id = l.Id,
                Name = l.Name,
                Address = l.Address,
                Capacity = l.Capacity
            }).ToList();
        return View(locations);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(LocationViewModel model)
    {
        if (ModelState.IsValid)
        {
            var location = new Location
            {
                Name = model.Name,
                Address = model.Address,
                Capacity = model.Capacity
            };
            _context.Locations.Add(location);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public IActionResult Edit(int id)
    {
        var location = _context.Locations.Find(id);
        if (location == null) return NotFound();
        var model = new LocationViewModel
        {
            Id = location.Id,
            Name = location.Name,
            Address = location.Address,
            Capacity = location.Capacity
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, LocationViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var location = _context.Locations.Find(id);
            if (location == null) return NotFound();
            location.Name = model.Name;
            location.Address = model.Address;
            location.Capacity = model.Capacity;
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public IActionResult Delete(int id)
    {
        var location = _context.Locations.Find(id);
        if (location == null) return NotFound();
        var model = new LocationViewModel
        {
            Id = location.Id,
            Name = location.Name,
            Address = location.Address,
            Capacity = location.Capacity
        };
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var location = _context.Locations.Find(id);
        if (location == null) return NotFound();
        _context.Locations.Remove(location);
        _context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }
}