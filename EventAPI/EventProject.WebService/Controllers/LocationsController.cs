using EventAPI.WebPlatformService.Services;
using EventProject.DTO.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EventAPI.WebPlatformService.Controllers
{
    [Route("[controller]")]
    public class LocationsController : Controller
    {
        private readonly IReferenceApiClient _referenceApiClient;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(
            IReferenceApiClient referenceApiClient,
            ILogger<LocationsController> logger)
        {
            _referenceApiClient = referenceApiClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _referenceApiClient.GetAllLocationsAsync();
                return View(result.OrderBy(r => r.Name)
                    .Select(r => new LocationViewModel
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Address = r.Address,
                        Capacity = r.Capacity
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading locations");
                return View(new List<LocationViewModel>());
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _referenceApiClient.GetLocationByIdAsync(id);
                return View(new LocationViewModel
                {
                    Id = result.Id,
                    Name = result.Name,
                    Address = result.Address,
                    Capacity = result.Capacity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location {Id}", id);
                return NotFound();
            }
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(LocationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var dto = new LocationDto
                {
                    Name = model.Name,
                    Address = model.Address,
                    Capacity = model.Capacity
                };

                var created = await _referenceApiClient.CreateLocationAsync(dto);
                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location");
                return View(model);
            }
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var location = await _referenceApiClient.GetLocationByIdAsync(id);
                return View(new LocationViewModel
                {
                    Id = location.Id,
                    Name = location.Name,
                    Address = location.Address,
                    Capacity = location.Capacity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location {Id}", id);
                return NotFound();
            }
        }

        [HttpPost("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, LocationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var dto = new LocationDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    Address = model.Address,
                    Capacity = model.Capacity
                };

                await _referenceApiClient.UpdateLocationAsync(id, dto);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location {Id}", id);
                return View(model);
            }
        }

        [HttpPost("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _referenceApiClient.DeleteLocationAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location {Id}", id);
                return RedirectToAction(nameof(Index));
            }
        }


    }
}