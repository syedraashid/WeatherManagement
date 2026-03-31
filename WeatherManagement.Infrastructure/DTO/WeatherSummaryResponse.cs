using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherManagement.Infrastructure.DTO
{
    public class WeatherSummaryResponse
    {
        public string City { get; set; } = string.Empty;
        public double AverageTemperature { get; set; }
        public double LowestTemperature { get; set; }
        public double HighestTemperature { get; set; }
        public double AverageHumidity { get; set; }
        public double AveragePressure { get; set; }
        public double AverageWindSpeed { get; set; }
        public int TotalEntries { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
