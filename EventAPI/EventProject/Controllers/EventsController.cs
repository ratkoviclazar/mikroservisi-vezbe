using EventAPI.Data;
using EventAPI.Domains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class EventsController : Controller
{
    private readonly EventDbContext _context;

    public EventsController(EventDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var events = _context.Events
            .Include(e => e.Location)
            .Include(e => e.EventType)
            .Select(e => new EventViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Agenda = e.Agenda,
                DateTime = e.DateTime,
                DurationInHours = e.DurationInHours,
                Price = e.Price,
                LocationId = e.LocationId,
                LocationName = e.Location.Name,
                TypeId = e.TypeId,
                TypeName = e.EventType.Name
            }).ToList();

        return View(events);
    }

    public IActionResult Details(int id)
    {
        var ev = _context.Events
            .Include(e => e.Location)
            .Include(e => e.EventType)
            .Where(e => e.Id == id)
            .Select(e => new EventViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Agenda = e.Agenda,
                DateTime = e.DateTime,
                DurationInHours = e.DurationInHours,
                Price = e.Price,
                LocationId = e.LocationId,
                LocationName = e.Location.Name,
                TypeId = e.TypeId,
                TypeName = e.EventType.Name
            }).FirstOrDefault();

        if (ev == null) return NotFound();

        return View(ev);
    }

    public IActionResult Create()
    {
        ViewBag.Locations = _context.Locations.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToList();
        ViewBag.Types = _context.EventTypes.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name }).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(EventViewModel model)
    {
        if (ModelState.IsValid)
        {
            var ev = new Event
            {
                Name = model.Name,
                Agenda = model.Agenda,
                DateTime = model.DateTime,
                DurationInHours = model.DurationInHours,
                Price = model.Price,
                LocationId = model.LocationId,
                TypeId = model.TypeId
            };
            _context.Events.Add(ev);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Field: {state.Key}, Error: {error.ErrorMessage}");
                }
            }
        }
        ViewBag.Locations = _context.Locations.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToList();
        ViewBag.Types = _context.EventTypes.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name }).ToList();
        return View(model);
    }

    public IActionResult Edit(int id)
    {
        var ev = _context.Events.Find(id);
        if (ev == null) return NotFound();

        var model = new EventViewModel
        {
            Id = ev.Id,
            Name = ev.Name,
            Agenda = ev.Agenda,
            DateTime = ev.DateTime,
            DurationInHours = ev.DurationInHours,
            Price = ev.Price,
            LocationId = ev.LocationId,
            TypeId = ev.TypeId
        };
        ViewBag.Locations = _context.Locations.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToList();
        ViewBag.Types = _context.EventTypes.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name }).ToList();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, EventViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var ev = _context.Events.Find(id);
            if (ev == null) return NotFound();

            ev.Name = model.Name;
            ev.Agenda = model.Agenda;
            ev.DateTime = model.DateTime;
            ev.DurationInHours = model.DurationInHours;
            ev.DurationInHours = model.DurationInHours;
            ev.LocationId = model.LocationId;
            ev.TypeId = model.TypeId;

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Locations = _context.Locations.Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToList();
        ViewBag.Types = _context.EventTypes.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name }).ToList();
        return View(model);
    }

    public IActionResult Delete(int id)
    {
        var ev = _context.Events
            .Include(e => e.Location)
            .Include(e => e.EventType)
            .Where(e => e.Id == id)
            .Select(e => new EventViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Agenda = e.Agenda,
                DateTime = e.DateTime,
                DurationInHours = e.DurationInHours,
                Price = e.Price,
                LocationName = e.Location.Name,
                TypeName = e.EventType.Name
            }).FirstOrDefault();

        if (ev == null) return NotFound();

        return View(ev);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var ev = _context.Events.Find(id);
        if (ev == null) return NotFound();

        _context.Events.Remove(ev);
        _context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }
}
