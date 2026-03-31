using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using WeatherManagement.Domain.Entities;
using WeatherManagement.Infrastructure.Configuration;
using WeatherManagement.Infrastructure.Data;
using WeatherManagement.Infrastructure.Integration;
using WeatherManagement.Infrastructure.Job;
using WeatherManagement.Infrastructure.Repo;

namespace WeatherManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Settings
        services.AddOptions<WeatherSettings>()
            .Bind(configuration.GetSection(WeatherSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Database
        services.AddDbContext<WeatherDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null)));

        // Generic repositories
        // WeatherData.Id is long — must be long here, not int
        services.AddScoped<IGenericRepository<WeatherData, long>>(sp =>
            new GenericRepository<WeatherData, long>(sp.GetRequiredService<WeatherDbContext>()));

        services.AddScoped<IGenericRepository<Location, int>>(sp =>
            new GenericRepository<Location, int>(sp.GetRequiredService<WeatherDbContext>()));

        // Refit client
        services.AddRefitClient<IWeatherApi>()
            .ConfigureHttpClient(c =>
                c.BaseAddress = new Uri("https://api.openweathermap.org"));

        // Orchestrator — write/sync path
        services.AddScoped<IWeatherOrchestratorService, WeatherOrchestratorService>();


        // Hangfire job
        services.AddScoped<WeatherFetchJob>();

        // Hangfire with PostgreSQL storage
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options => options.WorkerCount = 2);

        return services;
    }
}