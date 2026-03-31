using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WeatherManagement.Core.Service;
using WeatherManagement.Infrastructure.Integration;

namespace WeatherManagement.Api.Controllers
{
    [ApiController]
    [Route("api/weather")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IWeatherOrchestratorService _orchestrator;

        public WeatherController(
            IWeatherService weatherService,
            IWeatherOrchestratorService orchestrator)
        {
            _weatherService = weatherService;
            _orchestrator = orchestrator;
        }


        [HttpGet]
        public async Task<IActionResult> RetrieveAll()
        {
            var records = await _weatherService.ListAllRecordsAsync();
            return Ok(records);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> RetrieveById(long id)
        {
            var record = await _weatherService.FindRecordByIdAsync(id);
            if (record == null)
                return NotFound(new { message = $"Weather record {id} not found." });
            return Ok(record);
        }


        [HttpGet("current")]
        public async Task<IActionResult> RetrieveAllCurrent()
        {
            var records = await _weatherService.FetchAllCurrentAsync();
            return Ok(records);
        }

        [HttpGet("current/{city}")]
        public async Task<IActionResult> RetrieveCurrentByCity(string city)
        {
            var record = await _weatherService.FetchCurrentByCityAsync(city);
            if (record == null)
                return NotFound(new { message = $"No current weather found for city: {city}" });
            return Ok(record);
        }

        [HttpGet("history/{city}")]
        public async Task<IActionResult> RetrieveHistory(string city, [FromQuery] int days = 7)
        {
            var records = await _weatherService.RetrieveHistoryAsync(city, days);
            return Ok(records);
        }

        [HttpGet("compare")]
        public async Task<IActionResult> RetrieveComparison([FromQuery] List<string> cities)
        {
            if (cities == null || cities.Count < 2)
                return BadRequest(new { message = "Provide at least 2 cities. Usage: ?cities=London&cities=Paris" });

            var result = await _weatherService.BuildComparisonAsync(cities);
            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> RetrieveSummary([FromQuery] string city, [FromQuery] int days = 7)
        {
            if (string.IsNullOrEmpty(city))
                return BadRequest(new { message = "city is required. Usage: ?city=London" });

            var result = await _weatherService.ComputeSummaryAsync(city, days);
            if (result == null)
                return NotFound(new { message = $"No summary data found for city: {city}" });
            return Ok(result);
        }

        [HttpPost("sync")]
        public async Task<IActionResult> TriggerSync()
        {
            await _orchestrator.FetchAndStoreAsync();
            return Ok(new { message = "Weather sync completed successfully." });
        }
    }
}
