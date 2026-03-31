using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherManagement.Domain.Entities;
using WeatherManagement.Infrastructure.Configuration;
using WeatherManagement.Infrastructure.Data;

namespace WeatherManagement.Infrastructure.DataSeed
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var settings = config
                .GetSection("WeatherSettings")
                .Get<WeatherSettings>();

            if (settings?.Locations == null || !settings.Locations.Any())
                return;

            foreach (var loc in settings.Locations)
            {
                var exists = await db.Locations.AnyAsync(x =>
                    x.City == loc.City &&
                    x.CountryCode == loc.CountryCode);

                if (!exists)
                {
                    db.Locations.Add(new Location
                    {
                        City = loc.City,
                        CountryCode = loc.CountryCode,
                        Latitude = loc.Latitude,
                        Longitude = loc.Longitude,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
