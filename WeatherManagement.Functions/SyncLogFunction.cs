using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace WeatherManagement.Functions;

public class SyncLogFunction
{
    private readonly CosmosClient _cosmos;
    private readonly ILogger<SyncLogFunction> _logger;

    public SyncLogFunction(CosmosClient cosmos, ILogger<SyncLogFunction> logger)
    {
        _cosmos = cosmos;
        _logger = logger;
    }

    [Function("LogSync")]
    public async Task<HttpResponseData> RunAsync(
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
            return bad;
        }

        var entry = new SyncLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            SyncedAt = DateTime.Now,
            TriggeredBy = body?.TriggeredBy ?? "unknown",
            LocationCount = body?.LocationCount ?? 0,
            Status = "Completed"
        };

        var container = _cosmos.GetContainer("weatherlogs", "synclogs");
        await container.CreateItemAsync(entry, new PartitionKey(entry.Id));

        _logger.LogInformation(
            "Sync log recorded — TriggeredBy: {TriggeredBy}, Locations: {Count}, At: {SyncedAt}",
            entry.TriggeredBy, entry.LocationCount, entry.SyncedAt);

        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
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
