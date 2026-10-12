using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RideMatching.Application.Interfaces;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;
using RideMatching.Domain.ValueObjects;

namespace RideMatching.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        RideMatchingDbContext context,
        IDriverLocationStore locationStore,
        ILogger logger)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Drivers.AnyAsync())
        {
            // Sync existing available drivers into location store
            var availableDrivers = await context.Drivers
                .Where(d => d.Status == DriverStatus.Available)
                .ToListAsync();

            foreach (var driver in availableDrivers)
            {
                await locationStore.UpdateLocationAsync(driver.Id, driver.CurrentLatitude, driver.CurrentLongitude);
            }
            return;
        }

        logger.LogInformation("Seeding initial riders and drivers...");

        var riders = new List<Rider>
        {
            new("Alice Johnson", "alice@example.com", "+1-555-0101"),
            new("Bob Smith", "bob@example.com", "+1-555-0102"),
            new("Charlie Davis", "charlie@example.com", "+1-555-0103"),
            new("Diana Prince", "diana@example.com", "+1-555-0104")
        };

        await context.Riders.AddRangeAsync(riders);

        // Seed realistic drivers in San Francisco downtown
        var drivers = new List<Driver>
        {
            new(
                "Michael Chen",
                "michael.chen@rides.com",
                "+1-555-0201",
                new VehicleDetails("Toyota", "Camry Hybrid", "7XYZ890", "Silver", VehicleType.Economy),
                37.7749, -122.4194) // Civic Center
            {
            },
            new(
                "Sarah Miller",
                "sarah.miller@rides.com",
                "+1-555-0202",
                new VehicleDetails("Tesla", "Model 3", "8ABC123", "Midnight Black", VehicleType.Comfort),
                37.7879, -122.4075) // Union Square
            {
            },
            new(
                "Carlos Rodriguez",
                "carlos.r@rides.com",
                "+1-555-0203",
                new VehicleDetails("Hyundai", "Ioniq 5", "6DEF456", "Cyber Gray", VehicleType.Economy),
                37.7915, -122.3995) // Financial District / Embarcadero
            {
            },
            new(
                "Amina Al-Mansoor",
                "amina.m@rides.com",
                "+1-555-0204",
                new VehicleDetails("BMW", "530e", "9GHI789", "Alpine White", VehicleType.Premium),
                37.7600, -122.4200) // Mission District
            {
            },
            new(
                "David Kim",
                "david.kim@rides.com",
                "+1-555-0205",
                new VehicleDetails("Chevrolet", "Suburban", "5JKL012", "Dark Blue", VehicleType.XL),
                37.7833, -122.4167) // SoMa
            {
            }
        };

        foreach (var driver in drivers)
        {
            driver.GoOnline();
            driver.UpdateLocation(driver.CurrentLatitude, driver.CurrentLongitude, "San Francisco Metro");
        }

        await context.Drivers.AddRangeAsync(drivers);
        await context.SaveChangesAsync();

        foreach (var driver in drivers)
        {
            await locationStore.UpdateLocationAsync(driver.Id, driver.CurrentLatitude, driver.CurrentLongitude);
        }

        logger.LogInformation("Database seeded with {RiderCount} riders and {DriverCount} online drivers.",
            riders.Count, drivers.Count);
    }
}
