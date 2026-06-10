using EventAPI.WebPlatformService.Services;
using EventProject.LecturerService.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventProject.WebService.Controllers
{
    [Route("[controller]")]
    public class LecturersController : Controller
    {
        private readonly ILecturerApiClient _lecturerApiClient;
        private readonly ILogger<LecturersController> _logger;

        public LecturersController(
            ILecturerApiClient lecturerApiClient,
            ILogger<LecturersController> logger)
        {
            _lecturerApiClient = lecturerApiClient;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var lecturers = await _lecturerApiClient.GetAllLecturersAsync();
                return View(lecturers.OrderBy(l => l.Name)
                    .ThenBy(l => l.Surname)
                    .Select(l => new LecturerViewModel
                    {
                        Id = l.Id,
                        FirstName = l.Name,
                        LastName = l.Surname,
                        Title = l.Title,
                        ExpertiseArea = l.ExpertiseArea
                    })
                    .ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lecturers");
                ModelState.AddModelError("", "Greška pri učitavanju predavača");
                return View(new List<LecturerViewModel>());
            }
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var lecturer = await _lecturerApiClient.GetLecturerByIdAsync(id);
                return View(new LecturerViewModel
                {
                    Id = lecturer.Id,
                    FirstName = lecturer.Name,
                    LastName = lecturer.Surname,
                    Title = lecturer.Title,
                    ExpertiseArea = lecturer.ExpertiseArea
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lecturer {Id}", id);
                return NotFound("Predavač nije pronađen");
            }
        }


        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(LecturerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var dto = new LecturerDto
                {
                    Name = model.FirstName,
                    Surname = model.LastName,
                    Title = model.Title,
                    ExpertiseArea = model.ExpertiseArea
                };

                var created = await _lecturerApiClient.CreateLecturerAsync(dto);

                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lecturer");
                ModelState.AddModelError("", "Greška pri kreiranju predavača");
                return View(model);
            }
        }


        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var lecturer = await _lecturerApiClient.GetLecturerByIdAsync(id);

                var updateLecturer = new LecturerViewModel
                {
                    Id = lecturer.Id,
                    FirstName = lecturer.Name,
                    LastName = lecturer.Surname,
                    Title = lecturer.Title,
                    ExpertiseArea = lecturer.ExpertiseArea
                };

                return View(updateLecturer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lecturer {Id}", id);
                return NotFound("Predavač nije pronađen");
            }
        }

        [HttpPost("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, LecturerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var dto = new LecturerDto
                {
                    Name = model.FirstName,
                    Surname = model.LastName,
                    Title = model.Title,
                    ExpertiseArea = model.ExpertiseArea
                };

                await _lecturerApiClient.UpdateLecturerAsync(id, dto);

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lecturer {Id}", id);
                ModelState.AddModelError("", "Greška pri ažuriranju predavača");
                return View(model);
            }
        }


        [HttpPost("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _lecturerApiClient.DeleteLecturerAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lecturer {Id}", id);
                ModelState.AddModelError("", "Greška pri brisanju predavača");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}