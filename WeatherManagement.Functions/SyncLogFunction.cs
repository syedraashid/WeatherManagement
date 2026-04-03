using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace WeatherManagement.Functions;

public class SyncLogFunction
{
    private readonly ILogger<SyncLogFunction> _logger;

    public SyncLogFunction(ILogger<SyncLogFunction> logger)
    {
        _logger = logger;
    }

    [Function("LogSync")]
    [CosmosDBOutput(
        databaseName: "weatherlogs",
        containerName: "synclogs",
        Connection = "CosmosDb__ConnectionString",
        CreateIfNotExists = true)]
    public async Task<SyncLogEntry?> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/log")] HttpRequestData req)
    {
        SyncRequest? body = null;

        try
        {
            body = await JsonSerializer.DeserializeAsync<SyncRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid request body.");
            return null;
        }

        var entry = new SyncLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            SyncedAt = DateTime.Now,
            TriggeredBy = body?.TriggeredBy ?? "unknown",
            LocationCount = body?.LocationCount ?? 0,
            Status = "Completed"
        };

        _logger.LogInformation(
            "Sync log recorded — TriggeredBy: {TriggeredBy}, Locations: {Count}, At: {SyncedAt}",
            entry.TriggeredBy, entry.LocationCount, entry.SyncedAt);

        return entry;
    }
}

public record SyncRequest(string TriggeredBy, int LocationCount);

public class SyncLogEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public DateTime SyncedAt { get; init; } = DateTime.Now;
    public string TriggeredBy { get; init; } = "unknown";
    public int LocationCount { get; init; }
    public string Status { get; init; } = "Completed";
}
