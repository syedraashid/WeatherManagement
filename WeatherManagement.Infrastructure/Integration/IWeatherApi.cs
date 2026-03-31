using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherManagement.Infrastructure.Integration.Dto;

namespace WeatherManagement.Infrastructure.Integration
{
    public interface IWeatherApi
    {
        [Get("/data/3.0/onecall/day_summary")]
        Task<OpenWeatherResponse> GetDailySummaryAsync(
            [AliasAs("lat")] double lat,
            [AliasAs("lon")] double lon,
            [AliasAs("date")] string date,
            [AliasAs("appid")] string apiKey,
            [AliasAs("units")] string units = "metric"
        );
    }
}
