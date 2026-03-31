using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeatherManagement.Infrastructure.Integration.Dto
{
    public class OpenWeatherResponse
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("tz")]
        public string? Tz { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("units")]
        public string? Units { get; set; }

        [JsonPropertyName("cloud_cover")]
        public CloudCoverResponse? CloudCover { get; set; }

        [JsonPropertyName("humidity")]
        public HumidityResponse? Humidity { get; set; }

        [JsonPropertyName("precipitation")]
        public PrecipitationResponse? Precipitation { get; set; }

        [JsonPropertyName("temperature")]
        public TemperatureResponse? Temperature { get; set; }

        [JsonPropertyName("pressure")]
        public PressureResponse? Pressure { get; set; }

        [JsonPropertyName("wind")]
        public WindResponse? Wind { get; set; }
    }

    public class CloudCoverResponse
    {
        [JsonPropertyName("afternoon")]
        public double Afternoon { get; set; }
    }

    public class HumidityResponse
    {
        [JsonPropertyName("afternoon")]
        public double Afternoon { get; set; }
    }

    public class PrecipitationResponse
    {
        [JsonPropertyName("total")]
        public double Total { get; set; }
    }

    public class TemperatureResponse
    {
        [JsonPropertyName("min")]
        public double Min { get; set; }

        [JsonPropertyName("max")]
        public double Max { get; set; }

        [JsonPropertyName("afternoon")]
        public double Afternoon { get; set; }

        [JsonPropertyName("night")]
        public double Night { get; set; }

        [JsonPropertyName("evening")]
        public double Evening { get; set; }

        [JsonPropertyName("morning")]
        public double Morning { get; set; }
    }

    public class PressureResponse
    {
        [JsonPropertyName("afternoon")]
        public double Afternoon { get; set; }
    }

    public class WindResponse
    {
        [JsonPropertyName("max")]
        public WindMaxResponse? Max { get; set; }
    }

    public class WindMaxResponse
    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("direction")]
        public double Direction { get; set; }
    }
}
