namespace WeatherManagement.Infrastructure.Configuration
{
    public class WeatherSettings
    {
        public const string SectionName = "WeatherSettings";
        public string ApiKey { get; set; } = string.Empty;

        public int FetchIntervalMinutes { get; set; }

        public int MaxConcurrentRequests { get; set; }

        public List<LocationSetting> Locations { get; set; } = new();
    }

    public class LocationSetting
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
