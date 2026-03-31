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

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Read service
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<WeatherDbContext>();
    await DbSeeder.SeedAsync(services);
}

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
