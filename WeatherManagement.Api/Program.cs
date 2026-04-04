using Azure.Identity;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WeatherManagement.Api.Middleware;
using WeatherManagement.Core.Service;
using WeatherManagement.Infrastructure;
using WeatherManagement.Infrastructure.Configuration;
using WeatherManagement.Infrastructure.Data;
using WeatherManagement.Infrastructure.DataSeed;
using WeatherManagement.Infrastructure.Job;

var builder = WebApplication.CreateBuilder(args);

// Azure Key Vault — only when a vault URI is configured
var keyVaultUri = builder.Configuration["Azure:KeyVaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());

    // Re-add env vars after Key Vault so docker-compose overrides always win locally
    builder.Configuration.AddEnvironmentVariables();
}

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);

    var cosmosEndpoint = ctx.Configuration["CosmosDb:Endpoint"];
    if (!string.IsNullOrWhiteSpace(cosmosEndpoint))
    {
        lc.WriteTo.AzureCosmosDB(
            endpointUri: new Uri(cosmosEndpoint),
            authorizationKey: ctx.Configuration["CosmosDb:AuthKey"]!,
            databaseName: ctx.Configuration["CosmosDb:Database"] ?? "weatherlogs",
            collectionName: ctx.Configuration["CosmosDb:Collection"] ?? "applogs");
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Read service
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<WeatherDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(services);
}

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new LocalRequestsOnlyAuthorizationFilter() }
});
app.UseAuthorization();
app.MapControllers();

// Register recurring job via DI-based API (not static RecurringJob) so that
// JobStorage is guaranteed to be initialized before this call.
var weatherSettings = app.Configuration
    .GetSection(WeatherSettings.SectionName)
    .Get<WeatherSettings>();

var intervalMinutes = weatherSettings?.FetchIntervalMinutes ?? 30;
var cronExpression = intervalMinutes == 1
    ? "* * * * *"
    : $"*/{intervalMinutes} * * * *";

var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<WeatherFetchJob>(
    recurringJobId: "weather-fetch",
    methodCall: job => job.ExecuteAsync(),
    cronExpression: cronExpression);

app.Run();
