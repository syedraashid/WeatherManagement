using Hangfire;
using WeatherManagement.Api.Middleware;
using WeatherManagement.Core.Service;
using WeatherManagement.Infrastructure;
using WeatherManagement.Infrastructure.Configuration;
using WeatherManagement.Infrastructure.Job;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Read service
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

var weatherSettings = builder.Configuration
    .GetSection(WeatherSettings.SectionName)
    .Get<WeatherSettings>();

var intervalMinutes = weatherSettings?.FetchIntervalMinutes ?? 30;
var cronExpression = intervalMinutes == 1
    ? "* * * * *"
    : $"*/{intervalMinutes} * * * *";

RecurringJob.AddOrUpdate<WeatherFetchJob>(
    recurringJobId: "weather-fetch",
    methodCall: job => job.ExecuteAsync(),
    cronExpression: cronExpression);


app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseHangfireDashboard("/hangfire");
app.UseAuthorization();
app.MapControllers();

app.Run();