using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using WeatherManagement.Domain.Contracts;

namespace WeatherManagement.Infrastructure.Services;

public class AzureFunctionSyncNotifier : ISyncNotifier
{
    private readonly HttpClient _http;
    private readonly string? _functionUrl;
    private readonly ILogger<AzureFunctionSyncNotifier> _logger;

    public AzureFunctionSyncNotifier(
        HttpClient http,
        IConfiguration config,
        ILogger<AzureFunctionSyncNotifier> logger)
    {
        _http = http;
        _functionUrl = config["AzureFunction:SyncLogUrl"];
        _logger = logger;
    }

    public async Task NotifyAsync(string triggeredBy, int locationCount)
    {
        if (string.IsNullOrWhiteSpace(_functionUrl))
        {
            _logger.LogDebug("AzureFunction:SyncLogUrl not configured — skipping sync notification.");
            return;
        }

        var payload = JsonSerializer.Serialize(new { triggeredBy, locationCount });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync(_functionUrl, content);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Sync log sent to Azure Function for '{TriggeredBy}'.", triggeredBy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify Azure Function — sync log not recorded.");
        }
    }
}
