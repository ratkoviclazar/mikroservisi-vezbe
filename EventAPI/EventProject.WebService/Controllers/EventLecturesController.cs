using EventAPI.WebPlatformService.Patterns;
using EventAPI.WebPlatformService.Services;
using EventProject.DTO.DTOs;
using EventProject.LecturerService.Models;
using EventProject.WebService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventAPI.WebPlatformService.Controllers
{
    [Route("events/{eventId:int}/lectures")]
    public class EventLecturesController : Controller
    {
        private readonly IEventApiClient _eventApiClient;
        private readonly ILecturerApiClient _lecturerApiClient;
        private readonly ILogger<EventLecturesController> _logger;

        public EventLecturesController(
            IEventApiClient eventApiClient,
            ILecturerApiClient lecturerApiClient,
            ILogger<EventLecturesController> logger)
        {
            _lecturerApiClient = lecturerApiClient;
            _eventApiClient = eventApiClient;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            try
            {
                var lectures = await _eventApiClient.GetEventLecturesByEventIdAsync(eventId);
                var eventData = await _eventApiClient.GetEventByIdAsync(eventId);
                ViewBag.EventId = eventId;
                ViewBag.Event = eventData != null ? $"{eventData.Name} ({eventData.DateTime:dd MMM yyyy})" : "Unknown Event";
                return View(lectures.OrderBy(l => l.DateTime)
                    .Select(l => new EventLectureViewModel
                    {
                        Id = l.Id,
                        DateTime = l.DateTime,
                        DurationInHours = l.DurationInHours,
                        EventId = l.EventId,
                        EventName = l.Event?.Name ?? "",
                        LecturerId = l.LecturerId,
                        LecturerName = $"{l.Lecturer?.Name} {l.Lecturer?.Surname}"
                    }));
            }
            catch (CircuitBreakerOpenException ex)
            {
                _logger.LogWarning(ex,
                    "EventService circuit breaker is open (lectures).");

                ViewBag.ErrorMessage =
                    "Event service is not available at the moment.";

                return View(new List<EventLectureViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lectures for event {Id}", eventId);
                return View(new List<EventLectureViewModel>());
            }
        }
        [HttpGet("create")]
        public async Task<IActionResult> Create(int eventId)
        {
            try
            {

                var eventData = await _eventApiClient.GetEventByIdAsync(eventId);

                var lecturers = await _lecturerApiClient.GetAllLecturersAsync()
                        ?? new List<LecturerDto>();

                ViewBag.EventId = eventId;
                ViewBag.Event = eventData != null ? $"{eventData.Name} ({eventData.DateTime:dd MMM yyyy})" : "Unknown Event";

                ViewBag.AllLecturers = lecturers
                    .Select(l => new { l.Id, FullName = $"{l.Name} {l.Surname}" })
                    .ToList();


                return View(new EventLectureViewModel
                {
                    EventId = eventId,
                });
            }
            catch (CircuitBreakerOpenException ex)
            {
                _logger.LogWarning(ex,
                    "EventService circuit breaker is open (create lecture).");

                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error loading create lecture page for event {Id}", eventId);

                return RedirectToAction(nameof(Index), new { eventId });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(int eventId, EventLectureViewModel model)
        {
            try
            {
                var dto = new CreateEventLectureDto
                {
                    EventId = model.EventId,
                    LecturerId = model.LecturerId,
                    DateTime = model.DateTime,
                    DurationInHours = model.DurationInHours
                };

                dto.EventId = eventId;

                await _eventApiClient.CreateEventLectureAsync(dto);

                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (CircuitBreakerOpenException ex)
            {
                _logger.LogWarning(ex,
                    "EventService circuit breaker is open (create lecture).");

                TempData["ErrorMessage"] =
                    "Event service is not available at the moment.";

                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lecture for event {Id}", eventId);
                return RedirectToAction(nameof(Index), new { eventId });
            }
        }

        [HttpPost("delete/{lectureId:int}")]
        public async Task<IActionResult> Delete(int eventId, int lectureId)
        {
            try
            {
                await _eventApiClient.DeleteEventLectureAsync(lectureId);
                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (CircuitBreakerOpenException ex)
            {
                _logger.LogWarning(ex,
                    "EventService circuit breaker is open (delete lecture).");

                TempData["ErrorMessage"] =
                    "Event service is not available at the moment.";

                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lecture {Id}", lectureId);
                return RedirectToAction(nameof(Index), new { eventId });
            }
        }

    }
}
