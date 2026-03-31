using System.Text.Json.Serialization;

namespace WeatherManagement.Infrastructure.Integration.Dto
{
    public class CurrentWeatherResponse
    {
        [JsonPropertyName("weather")]
        public List<WeatherCondition>? Weather { get; set; }

        [JsonPropertyName("main")]
        public MainData? Main { get; set; }

        [JsonPropertyName("wind")]
        public WindData? Wind { get; set; }

        [JsonPropertyName("dt")]
        public long Dt { get; set; }
    }

    public class WeatherCondition
    {
        [JsonPropertyName("main")]
        public string? Main { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class MainData
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }
    }

    public class WindData
    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; }
    }
}
