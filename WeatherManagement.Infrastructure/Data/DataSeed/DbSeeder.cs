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
            var db = services.GetRequiredService<WeatherDbContext>();
            var config = services.GetRequiredService<IConfiguration>();

            var settings = config
                .GetSection("WeatherSettings")
                .Get<WeatherSettings>();

            if (settings?.Locations == null || !settings.Locations.Any())
                return;

            foreach (var loc in settings.Locations)
            {
                var exists = await db.Locations.AnyAsync(x =>
                    x.City.ToLower() == loc.City.ToLower() &&
                    x.CountryCode.ToLower() == loc.CountryCode.ToLower());

                if (!exists)
                {
                    db.Locations.Add(new Location
                    {
                        City = loc.City,
                        Country = loc.Country,
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
