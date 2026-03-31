using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherManagement.Infrastructure.DTO
{
    public class LocationStatusResponse
    {
        public string City { get; set; } = string.Empty;
        public bool IsOperational { get; set; }
        public string LastKnownStatus { get; set; } = string.Empty;
        public DateTime? LastSyncedAt { get; set; }
        public int RecentSuccessCount { get; set; }
        public int RecentFailureCount { get; set; }
        public double SuccessRate { get; set; }
    }
}
