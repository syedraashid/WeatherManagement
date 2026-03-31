using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherManagement.Infrastructure.DTO
{
    public class UpdateLocationRequest
    {
        public string? CountryCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool? IsActive { get; set; }
    }
}
