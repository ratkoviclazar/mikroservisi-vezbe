using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventAPI.ApiGateway.Controllers
{
    [ApiController]
    [Route("gateway/v1/composite")]
    public class CompositeController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CompositeController> _logger;

        public CompositeController(
            IHttpClientFactory httpClientFactory,
            ILogger<CompositeController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet("events/{id:int}")]
        public async Task<IActionResult> GetEventComposite(int id)
        {
            var client = _httpClientFactory.CreateClient("composition");

            var eventUrl = $"http://localhost:5024/api/events/{id}";
            var locationsUrl = "http://localhost:5240/api/locations";
            var eventTypesUrl = "http://localhost:5240/api/event-types";
            var lecturersUrl = "http://localhost:5129/api/lecturers";

            _logger.LogInformation("Calling Event Service: {Url}", eventUrl);

            HttpResponseMessage eventResponse;

            try
            {
                eventResponse = await client.GetAsync(eventUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event Service is not available.");

                return StatusCode(503, new
                {
                    error = "Event Service is not available.",
                    url = eventUrl,
                    details = ex.Message
                });
            }

            if (!eventResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)eventResponse.StatusCode, new
                {
                    error = $"Event with id {id} was not found or Event Service returned an error.",
                    url = eventUrl,
                    statusCode = (int)eventResponse.StatusCode
                });
            }

            var eventJson = await eventResponse.Content.ReadAsStringAsync();

            var locationsJson = await SafeGetJsonAsync(client, locationsUrl);
            var eventTypesJson = await SafeGetJsonAsync(client, eventTypesUrl);
            var lecturersJson = await SafeGetJsonAsync(client, lecturersUrl);

            return Ok(new
            {
                eventId = id,
                eventData = JsonSerializer.Deserialize<object>(eventJson),
                referenceData = new
                {
                    locations = JsonSerializer.Deserialize<object>(locationsJson),
                    eventTypes = JsonSerializer.Deserialize<object>(eventTypesJson)
                },
                lecturers = JsonSerializer.Deserialize<object>(lecturersJson)
            });
        }

        private async Task<string> SafeGetJsonAsync(HttpClient client, string url)
        {
            try
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Request to {Url} failed with status code {StatusCode}",
                        url,
                        response.StatusCode);

                    return "[]";
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request to {Url} failed.", url);
                return "[]";
            }
        }
    }
}