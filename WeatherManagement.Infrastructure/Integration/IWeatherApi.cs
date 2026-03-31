using Refit;
using WeatherManagement.Infrastructure.Integration.Dto;

namespace WeatherManagement.Infrastructure.Integration
{
    public interface IWeatherApi
    {
        [Get("/data/2.5/weather")]
        Task<CurrentWeatherResponse> GetCurrentWeatherAsync(
             [AliasAs("lat")] double lat,
             [AliasAs("lon")] double lon,
             [AliasAs("appid")] string apiKey,
             [AliasAs("units")] string units
         );
    }
}
