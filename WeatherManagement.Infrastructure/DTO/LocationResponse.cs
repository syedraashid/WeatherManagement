using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherManagement.Infrastructure.DTO
{
    public class LocationResponse
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
