using EventAPI.WebPlatformService.Services;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;


namespace EventAPI.WebPlatformService.Controllers
{
    public class EventTypesController : Controller
    {
        private readonly IReferenceApiClient _referenceApiClient;
        private readonly ILogger<EventTypesController> _logger;

        public EventTypesController(
            IReferenceApiClient referenceApiClient,
            ILogger<EventTypesController> logger)
        {
            _referenceApiClient = referenceApiClient;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _referenceApiClient.GetAllEventTypesAsync();
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event types");
                return View(new List<EventTypeDto>());
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _referenceApiClient.GetEventTypeByIdAsync(id);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event type {Id}", id);
                return NotFound();
            }
        }

        [HttpGet("create")]
        public IActionResult Create(int eventId)
        {
            return View(new EventLectureViewModel
            {
                EventId = eventId
            });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(EventTypeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(dto);

                var created = await _referenceApiClient.CreateEventTypeAsync(dto);
                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event type");
                return View(dto);
            }
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var result = await _referenceApiClient.GetEventTypeByIdAsync(id);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event type {Id}", id);
                return NotFound();
            }
        }

        [HttpPost("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, EventTypeDto dto)
        {
            try
            {
                await _referenceApiClient.UpdateEventTypeAsync(id, dto);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event type {Id}", id);
                return View(dto);
            }
        }

        [HttpPost("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _referenceApiClient.DeleteEventTypeAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event type {Id}", id);
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
