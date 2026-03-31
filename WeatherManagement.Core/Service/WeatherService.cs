using Microsoft.Extensions.Logging;
using WeatherManagement.Domain.Entities;
using WeatherManagement.Infrastructure.DTO;
using WeatherManagement.Infrastructure.Repo;

namespace WeatherManagement.Core.Service
{
    public interface IWeatherService
    {
        Task<WeatherRecordResponse?> FindRecordByIdAsync(long id);
        Task<IEnumerable<WeatherRecordResponse>> ListAllRecordsAsync();
        Task<WeatherRecordResponse?> FetchCurrentByCityAsync(string city);
        Task<IEnumerable<WeatherRecordResponse>> FetchAllCurrentAsync();
        Task<IEnumerable<WeatherRecordResponse>> RetrieveHistoryAsync(string city, int days = 7);
        Task<WeatherComparisonResponse> BuildComparisonAsync(List<string> cities);
        Task<WeatherSummaryResponse?> ComputeSummaryAsync(string city, int days = 7);
    }

    public class WeatherService : IWeatherService
    {
        private readonly IGenericRepository<WeatherData, long> _weatherRepo;
        private readonly IGenericRepository<Location, int> _locationRepo;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(
            IGenericRepository<WeatherData, long> weatherRepo,
            IGenericRepository<Location, int> locationRepo,
            ILogger<WeatherService> logger)
        {
            _weatherRepo = weatherRepo;
            _locationRepo = locationRepo;
            _logger = logger;
        }

        public async Task<WeatherRecordResponse?> FindRecordByIdAsync(long id)
        {
            var record = await _weatherRepo.FindByIdAsync(id);
            if (record == null) return null;

            var location = await _locationRepo.FindByIdAsync(record.LocationId);
            return location == null ? null : MapToResponse(record, location);
        }

        public async Task<IEnumerable<WeatherRecordResponse>> ListAllRecordsAsync()
        {
            var records = await _weatherRepo.ListAllAsync();
            var locationMap = (await _locationRepo.ListAllAsync()).ToDictionary(l => l.Id);

            return records
                .Where(r => locationMap.ContainsKey(r.LocationId))
                .Select(r => MapToResponse(r, locationMap[r.LocationId]));
        }

        public async Task<WeatherRecordResponse?> FetchCurrentByCityAsync(string city)
        {
            var location = await _locationRepo.FindFirstAsync(
                l => l.City.ToLower() == city.ToLower() && l.IsActive);

            if (location == null) return null;

            var locationId = location.Id;
            var record = (await _weatherRepo.FindAsync(w => w.LocationId == locationId))
                .OrderByDescending(w => w.FetchedAt)
                .FirstOrDefault();

            return record == null ? null : MapToResponse(record, location);
        }

        public async Task<IEnumerable<WeatherRecordResponse>> FetchAllCurrentAsync()
        {
            var activeLocations = (await _locationRepo.FindAsync(l => l.IsActive))
                .ToDictionary(l => l.Id);

            var activeIds = activeLocations.Keys.ToArray();
            var allWeather = await _weatherRepo.FindAsync(w => activeIds.Contains(w.LocationId));

            return allWeather
                .GroupBy(w => w.LocationId)
                .Select(g => g.OrderByDescending(w => w.FetchedAt).First())
                .Select(w => MapToResponse(w, activeLocations[w.LocationId]));
        }

        public async Task<IEnumerable<WeatherRecordResponse>> RetrieveHistoryAsync(string city, int days = 7)
        {
            var location = await _locationRepo.FindFirstAsync(
                l => l.City.ToLower() == city.ToLower() && l.IsActive);

            if (location == null) return Enumerable.Empty<WeatherRecordResponse>();

            var fromDate = DateTime.UtcNow.AddDays(-days);
            var locationId = location.Id;
            var history = await _weatherRepo.FindAsync(
                w => w.LocationId == locationId && w.FetchedAt >= fromDate);

            return history
                .OrderByDescending(w => w.FetchedAt)
                .Select(w => MapToResponse(w, location));
        }

        public async Task<WeatherComparisonResponse> BuildComparisonAsync(List<string> cities)
        {
            var activeLocations = (await _locationRepo.FindAsync(l => l.IsActive)).ToList();
            var activeIds = activeLocations.Select(l => l.Id).ToArray();
            var allWeather = await _weatherRepo.FindAsync(w => activeIds.Contains(w.LocationId));

            var weatherByLocation = allWeather
                .GroupBy(w => w.LocationId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(w => w.FetchedAt).First());

            var results = cities
                .Select(city =>
                {
                    var location = activeLocations.FirstOrDefault(l =>
                        l.City.Equals(city, StringComparison.OrdinalIgnoreCase));
                    if (location == null) return null;

                    return weatherByLocation.TryGetValue(location.Id, out var record)
                        ? MapToResponse(record, location)
                        : null;
                })
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();

            return new WeatherComparisonResponse { Results = results, GeneratedAt = DateTime.UtcNow };
        }

        public async Task<WeatherSummaryResponse?> ComputeSummaryAsync(string city, int days = 7)
        {
            var location = await _locationRepo.FindFirstAsync(
                l => l.City.ToLower() == city.ToLower() && l.IsActive);

            if (location == null) return null;

            var fromDate = DateTime.UtcNow.AddDays(-days);
            var locationId = location.Id;
            var history = (await _weatherRepo.FindAsync(
                w => w.LocationId == locationId && w.FetchedAt >= fromDate)).ToList();

            if (history.Count == 0) return null;

            return new WeatherSummaryResponse
            {
                City = city,
                AverageTemperature = Math.Round(history.Average(d => d.Temperature), 2),
                LowestTemperature = Math.Round(history.Min(d => d.Temperature), 2),
                HighestTemperature = Math.Round(history.Max(d => d.Temperature), 2),
                AverageHumidity = Math.Round(history.Average(d => d.Humidity), 2),
                AveragePressure = Math.Round(history.Average(d => d.Pressure), 2),
                AverageWindSpeed = Math.Round(history.Average(d => d.WindSpeed), 2),
                TotalEntries = history.Count,
                PeriodStart = history.Min(d => d.FetchedAt),
                PeriodEnd = history.Max(d => d.FetchedAt)
            };
        }

        private static WeatherRecordResponse MapToResponse(WeatherData data, Location location) => new()
        {
            Id = data.Id,
            LocationId = data.LocationId,
            City = location.City,
            Country = location.Country,
            CountryCode = location.CountryCode,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Temperature = data.Temperature,
            Humidity = data.Humidity,
            Pressure = data.Pressure,
            Condition = data.Condition,
            WindSpeed = data.WindSpeed,
            RecordedAt = data.RecordedAt,
            FetchedAt = data.FetchedAt
        };
    }
}
