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

            var semaphore = new SemaphoreSlim(_settings.MaxConcurrentRequests);
            var results = new List<WeatherData>(locations.Count);

            var tasks = locations.Select(async loc =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var data = await ProcessLocation(loc);
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

        private async Task<WeatherData?> ProcessLocation(Location loc)
        {
            if (loc.Latitude == null || loc.Longitude == null)
            {
                _logger.LogWarning("Skipping location {City} - no coordinates", loc.City);
                return null;
            }

            try
            {
                var response = await _api.GetCurrentWeatherAsync(
                    loc.Latitude.Value,
                    loc.Longitude.Value,
                    _settings.ApiKey
                );

                if (response.Main == null || response.Wind == null)
                {
                    _logger.LogWarning("Incomplete weather data received for {City} — skipping", loc.City);
                    return null;
                }

                var condition = response.Weather?.FirstOrDefault()?.Main ?? "Unknown";

                return new WeatherData
                {
                    LocationId = loc.Id,
                    Temperature = response.Main.Temp,
                    Humidity = response.Main.Humidity,
                    Pressure = response.Main.Pressure,
                    WindSpeed = response.Wind.Speed,
                    Condition = condition,
                    RecordedAt = DateTimeOffset.FromUnixTimeSeconds(response.Dt).UtcDateTime,
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
