using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherManagement.Infrastructure.DTO
{
    public class WeatherRecordResponse
    {
        public long Id { get; set; }          
        public int LocationId { get; set; }
        public string City { get; set; } = string.Empty;     
        public string Country { get; set; } = string.Empty;     
        public string CountryCode { get; set; } = string.Empty; 
        public double? Latitude { get; set; }                  
        public double? Longitude { get; set; }                 
        public double Temperature { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }
        public string Condition { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public DateTime RecordedAt { get; set; }
        public DateTime FetchedAt { get; set; }
    }
}
