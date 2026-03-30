using EventAPI.Data;
using EventAPI.Domains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class EventLecturesController : Controller
{
    private readonly EventDbContext _context;

    public EventLecturesController(EventDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(int eventId)
    {
        var ev = _context.Events.Find(eventId);
        if (ev == null) return NotFound();

        ViewBag.EventName = ev.Name;
        ViewBag.EventId = eventId;

        var lectures = _context.EventLectures
            .Include(el => el.Lecturer)
            .Where(el => el.EventId == eventId)
            .Select(el => new EventLectureViewModel
            {
                Id = el.Id,
                EventId = el.EventId,
                LecturerId = el.LecturerId,
                LecturerName = el.Lecturer.Title + " " + el.Lecturer.Name + " " + el.Lecturer.Surname,
                DateTime = el.DateTime,
                DurationInHours = el.DurationInHours
            }).ToList();

        return View(lectures);
    }

    public IActionResult Create(int eventId)
    {
        var ev = _context.Events.Find(eventId);
        if (ev == null) return NotFound();

        ViewBag.EventId = eventId;
        ViewBag.EventName = ev.Name;
        ViewBag.Lecturers = _context.Lecturers
            .Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = l.Title + " " + l.Name + " " + l.Surname
            }).ToList();

        return View(new EventLectureViewModel
        {
            EventId = eventId,
            DateTime = ev.DateTime
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(EventLectureViewModel model)
    {
        if (ModelState.IsValid)
        {
            bool duplicate = _context.EventLectures.Any(el =>
                el.EventId == model.EventId &&
                el.LecturerId == model.LecturerId &&
                el.DateTime == model.DateTime);

            if (duplicate)
            {
                ModelState.AddModelError("", "Ovaj predavač već ima predavanje u isto vreme na ovom događaju.");
            }
            else
            {
                var eventLecture = new EventLecture
                {
                    EventId = model.EventId,
                    LecturerId = model.LecturerId,
                    DateTime = model.DateTime,
                    DurationInHours = model.DurationInHours
                };
                _context.EventLectures.Add(eventLecture);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index), new { eventId = model.EventId });
            }
        }

        var ev = _context.Events.Find(model.EventId);
        ViewBag.EventId = model.EventId;
        ViewBag.EventName = ev?.Name;
        ViewBag.Lecturers = _context.Lecturers
            .Select(l => new SelectListItem
            {
                Value = l.Id.ToString(),
                Text = l.Title + " " + l.Name + " " + l.Surname
            }).ToList();

        return View(model);
    }

    public IActionResult Delete(int id)
    {
        var el = _context.EventLectures
            .Include(x => x.Lecturer)
            .Include(x => x.Event)
            .FirstOrDefault(x => x.Id == id);

        if (el == null) return NotFound();

        var model = new EventLectureViewModel
        {
            Id = el.Id,
            EventId = el.EventId,
            EventName = el.Event.Name,
            LecturerId = el.LecturerId,
            LecturerName = el.Lecturer.Title + " " + el.Lecturer.Name + " " + el.Lecturer.Surname,
            DateTime = el.DateTime,
            DurationInHours = el.DurationInHours
        };

        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var el = _context.EventLectures.Find(id);
        if (el == null) return NotFound();

        int eventId = el.EventId;
        _context.EventLectures.Remove(el);
        _context.SaveChanges();

        return RedirectToAction(nameof(Index), new { eventId });
    }
}