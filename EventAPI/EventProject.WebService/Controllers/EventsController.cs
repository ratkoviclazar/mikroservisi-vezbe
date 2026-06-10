using EventAPI.WebPlatformService.Services;
using EventProject.DTO.DTOs;
using EventProject.WebService.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventProject.WebService.Controllers
{
    [Route("[controller]")]
    public class EventsController : Controller
    {
        private readonly IEventApiClient _eventApiClient;
        private readonly IReferenceApiClient _referenceApiClient;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            IReferenceApiClient referenceApiClient,
            IEventApiClient eventApiClient,
            ILogger<EventsController> logger)
        {
            _referenceApiClient = referenceApiClient;
            _eventApiClient = eventApiClient;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var events = await _eventApiClient.GetAllEventsAsync();
                return View(events.OrderBy(e => e.Name)
                    .Select(e => new EventViewModel
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Agenda = e.Agenda,
                        DateTime = e.DateTime,
                        DurationInHours = e.DurationInHours,
                        Price = e.Price,
                        TypeId = e.TypeId,
                        TypeName = e.EventType?.Name,
                        LocationId = e.LocationId,
                        LocationName = e.Location?.Name
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events");
                return View(new List<EventViewModel>());
            }
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var ev = await _eventApiClient.GetEventByIdAsync(id);
                return View(new EventViewModel
                {
                    Id = ev.Id,
                    Name = ev.Name,
                    Agenda = ev.Agenda,
                    DateTime = ev.DateTime,
                    DurationInHours = ev.DurationInHours,
                    Price = ev.Price,
                    TypeId = ev.TypeId,
                    TypeName = ev.EventType?.Name,
                    LocationId = ev.LocationId,
                    LocationName = ev.Location?.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event {Id}", id);
                return NotFound();
            }
        }


        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);
                var createDto = new CreateEventDto
                {
                    Name = model.Name,
                    Agenda = model.Agenda,
                    DateTime = model.DateTime,
                    DurationInHours = model.DurationInHours,
                    Price = model.Price,
                    TypeId = model.TypeId,
                    LocationId = model.LocationId
                };
                var created = await _eventApiClient.CreateEventAsync(createDto);

                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return View(model);
            }
        }


        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {

                var locations = await _referenceApiClient.GetAllLocationsAsync();
                var types = await _referenceApiClient.GetAllEventTypesAsync();
                ViewBag.Locations = locations.OrderBy(l => l.Name).ToList();
                ViewBag.Types = types.OrderBy(t => t.Name).ToList();

                var ev = await _eventApiClient.GetEventByIdAsync(id);

                var dto = new UpdateEventDto
                {
                    Name = ev.Name,
                    Agenda = ev.Agenda,
                    DateTime = ev.DateTime,
                    DurationInHours = ev.DurationInHours,
                    Price = ev.Price,
                    TypeId = ev.TypeId,
                    LocationId = ev.LocationId
                };

                ViewBag.EventId = id;
                return View(new EventViewModel
                {
                    Name = dto.Name,
                    Agenda = dto.Agenda,
                    DateTime = dto.DateTime,
                    DurationInHours = dto.DurationInHours,
                    Price = dto.Price,
                    TypeId = dto.TypeId,
                    LocationId = dto.LocationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event {Id}", id);
                return NotFound();
            }
        }

        [HttpPost("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, EventViewModel model)
        {
            try
            {


                if (!ModelState.IsValid)
                    return View(model);

                var dto = new UpdateEventDto
                {
                    Name = model.Name,
                    Agenda = model.Agenda,
                    DateTime = model.DateTime,
                    DurationInHours = model.DurationInHours,
                    Price = model.Price,
                    TypeId = model.TypeId,
                    LocationId = model.LocationId
                };

                await _eventApiClient.UpdateEventAsync(id, dto);

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {Id}", id);
                return View(model);
            }
        }


        [HttpPost("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _eventApiClient.DeleteEventAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {Id}", id);
                return RedirectToAction(nameof(Index));
            }
        }
    }
}