using System.ComponentModel.DataAnnotations;

namespace WeatherManagement.Domain.Entities
{
    public class WeatherData
    {
        public long Id { get; set; }

        [Required]
        public int LocationId { get; set; }

        public Location Location { get; set; }

        public double Temperature { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }

        [MaxLength(200)]
        public string Condition { get; set; }

        public double WindSpeed { get; set; }

        public DateTime RecordedAt { get; set; }
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    }
}