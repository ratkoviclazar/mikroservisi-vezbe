using EventAPI.Data;
using EventAPI.Domains;
using Microsoft.AspNetCore.Mvc;

public class LecturersController : Controller
{
    private readonly EventDbContext _context;

    public LecturersController(EventDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var lecturers = _context.Lecturers
            .Select(l => new LecturerViewModel
            {
                Id = l.Id,
                FirstName = l.Name,
                LastName = l.Surname,
                Title = l.Title,
                ExpertiseArea = l.ExpertiseArea
            }).ToList();

        return View(lecturers);
    }

    public IActionResult Details(int id)
    {
        var lecturer = _context.Lecturers
            .Where(l => l.Id == id)
            .Select(l => new LecturerViewModel
            {
                Id = l.Id,
                FirstName = l.Name,
                LastName = l.Surname,
                Title = l.Title,
                ExpertiseArea = l.ExpertiseArea
            }).FirstOrDefault();

        if (lecturer == null) return NotFound();

        return View(lecturer);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(LecturerViewModel model)
    {
        if (ModelState.IsValid)
        {
            var lecturer = new Lecturer
            {
                Name = model.FirstName,
                Surname = model.LastName,
                Title = model.Title,
                ExpertiseArea = model.ExpertiseArea
            };
            _context.Lecturers.Add(lecturer);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public IActionResult Edit(int id)
    {
        var lecturer = _context.Lecturers.Find(id);
        if (lecturer == null) return NotFound();

        var model = new LecturerViewModel
        {
            Id = lecturer.Id,
            FirstName = lecturer.Name,
            LastName = lecturer.Surname,
            Title = lecturer.Title,
            ExpertiseArea = lecturer.ExpertiseArea
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, LecturerViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var lecturer = _context.Lecturers.Find(id);
            if (lecturer == null) return NotFound();

            lecturer.Name = model.FirstName;
            lecturer.Surname = model.LastName;
            lecturer.Title = model.Title;
            lecturer.ExpertiseArea = model.ExpertiseArea;

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public IActionResult Delete(int id)
    {
        var lecturer = _context.Lecturers
            .Where(l => l.Id == id)
            .Select(l => new LecturerViewModel
            {
                Id = l.Id,
                FirstName = l.Name,
                LastName = l.Surname,
                Title = l.Title,
                ExpertiseArea = l.ExpertiseArea
            }).FirstOrDefault();

        if (lecturer == null) return NotFound();

        return View(lecturer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var lecturer = _context.Lecturers.Find(id);
        if (lecturer == null) return NotFound();

        _context.Lecturers.Remove(lecturer);
        _context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }
}