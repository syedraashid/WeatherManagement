using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherManagement.Domain.Entities;
using WeatherManagement.Infrastructure.Configuration;
using WeatherManagement.Infrastructure.Data;

namespace WeatherManagement.Infrastructure.Integration
{
    public interface IWeatherOrchestratorService
    {
        Task FetchAndStoreAsync();
    }

    public class WeatherOrchestratorService : IWeatherOrchestratorService
    {
        private readonly WeatherDbContext _db;
        private readonly IWeatherApi _api;
        private readonly WeatherSettings _settings;
        private readonly ILogger<WeatherOrchestratorService> _logger;

        public WeatherOrchestratorService(
            WeatherDbContext db,
            IWeatherApi api,
            IOptions<WeatherSettings> settings,
            ILogger<WeatherOrchestratorService> logger)
        {
            _db = db;
            _api = api;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task FetchAndStoreAsync()
        {
            var locations = await _db.Locations
                .Where(x => x.IsActive)
                .ToListAsync();

            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var semaphore = new SemaphoreSlim(_settings.MaxConcurrentRequests);
            var results = new List<WeatherData>(locations.Count);

            var tasks = locations.Select(async loc =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var data = await ProcessLocation(loc, date);
                    if (data != null)
                    {
                        lock (results)
                        {
                            results.Add(data);
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            _db.WeatherReadings.AddRange(results);
            await _db.SaveChangesAsync();
        }

        private async Task<WeatherData?> ProcessLocation(Location loc, string date)
        {
            if (loc.Latitude == null || loc.Longitude == null)
            {
                _logger.LogWarning("Skipping location {City} - no coordinates", loc.City);
                return null;
            }

            try
            {
                var response = await _api.GetDailySummaryAsync(
                    loc.Latitude.Value,
                    loc.Longitude.Value,
                    date,
                    _settings.ApiKey
                );

                if (response.Temperature == null || response.Humidity == null
                    || response.Pressure == null || response.Wind?.Max == null)
                {
                    _logger.LogWarning(
                        "Incomplete weather data received for {City} on {Date} — skipping",
                        loc.City, date);
                    return null;
                }

                return new WeatherData
                {
                    LocationId = loc.Id,
                    Temperature = response.Temperature.Afternoon,
                    Humidity = (int)response.Humidity.Afternoon,
                    Pressure = (int)response.Pressure.Afternoon,
                    WindSpeed = response.Wind.Max.Speed,
                    Condition = "Daily Summary",
                    RecordedAt = DateTime.UtcNow,
                    FetchedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch weather data for {City}", loc.City);
                return null;
            }
        }
    }
}
