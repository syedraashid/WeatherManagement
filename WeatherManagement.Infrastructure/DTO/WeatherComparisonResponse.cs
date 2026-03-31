using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherManagement.Infrastructure.DTO
{
    public class WeatherComparisonResponse
    {
        public List<WeatherRecordResponse> Results { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}