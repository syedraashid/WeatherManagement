using Hangfire;
using Microsoft.Extensions.Logging;
using WeatherManagement.Domain.Contracts;
using WeatherManagement.Infrastructure.Integration;

namespace WeatherManagement.Infrastructure.Job;

public class WeatherFetchJob
{
    private readonly IWeatherOrchestratorService _orchestrator;
    private readonly ISyncNotifier _syncNotifier;
    private readonly ILogger<WeatherFetchJob> _logger;

    public WeatherFetchJob(
        IWeatherOrchestratorService orchestrator,
        ISyncNotifier syncNotifier,
        ILogger<WeatherFetchJob> logger)
    {
        _orchestrator = orchestrator;
        _syncNotifier = syncNotifier;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Hangfire: Weather sync started at {Time}", DateTime.UtcNow);
        var count = await _orchestrator.FetchAndStoreAsync();
        _logger.LogInformation("Hangfire: Weather sync completed at {Time}", DateTime.UtcNow);

        await _syncNotifier.NotifyAsync("scheduled", count);
    }
}
