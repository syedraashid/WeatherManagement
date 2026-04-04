using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherManagement.Domain.Entities;

namespace WeatherManagement.Infrastructure.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
            : base(options)
        {
        }

        public DbSet<Location> Locations { get; set; }
        public DbSet<WeatherData> WeatherReadings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureLocation(modelBuilder);
            ConfigureWeatherReading(modelBuilder);
        }

        private void ConfigureLocation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.City)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.County)
                    .HasMaxLength(100);

                entity.Property(x => x.Country)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.CountryCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(x => x.Latitude);

                entity.Property(x => x.Longitude);

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.Property(x => x.CreatedAt);

                entity.HasIndex(x => new { x.City, x.CountryCode })
                    .IsUnique();
            });
        }

        private void ConfigureWeatherReading(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeatherData>(entity =>
            {
                entity.ToTable("WeatherReadings");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Temperature);

                entity.Property(x => x.Humidity);

                entity.Property(x => x.Pressure);

                entity.Property(x => x.Condition)
                    .HasMaxLength(200);

                entity.Property(x => x.WindSpeed);

                entity.Property(x => x.RecordedAt)
                    .IsRequired();

                entity.Property(x => x.FetchedAt)
                    .IsRequired();

                entity.HasOne(x => x.Location)
                    .WithMany(x => x.WeatherReadings)
                    .HasForeignKey(x => x.LocationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.LocationId, x.RecordedAt });
            });
        }
    }
}
