using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherManagement.Infrastructure.Integration;

namespace WeatherManagement.Infrastructure.Job
{
    public class WeatherFetchJob
    {
        private readonly IWeatherOrchestratorService _orchestrator;
        private readonly ILogger<WeatherFetchJob> _logger;

        public WeatherFetchJob(
            IWeatherOrchestratorService orchestrator,
            ILogger<WeatherFetchJob> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Hangfire: Weather sync started at {Time}", DateTime.UtcNow);
            await _orchestrator.FetchAndStoreAsync();
            _logger.LogInformation("Hangfire: Weather sync completed at {Time}", DateTime.UtcNow);
        }
    }
}
