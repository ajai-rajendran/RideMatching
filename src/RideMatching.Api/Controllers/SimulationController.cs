using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RideMatching.Application.Common;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;
using RideMatching.Infrastructure.Persistence;

namespace RideMatching.Api.Controllers;

public record DriverStepRequest(
    Guid DriverId,
    double TargetLatitude,
    double TargetLongitude,
    double StepFraction = 0.15);

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly RideMatchingDbContext _dbContext;
    private readonly IDriverService _driverService;
    private readonly IDriverLocationStore _locationStore;
    private readonly ILogger<SimulationController> _logger;

    public SimulationController(
        RideMatchingDbContext dbContext,
        IDriverService driverService,
        IDriverLocationStore locationStore,
        ILogger<SimulationController> logger)
    {
        _dbContext = dbContext;
        _driverService = driverService;
        _locationStore = locationStore;
        _logger = logger;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> ReSeed()
    {
        // Clear existing tables
        _dbContext.Rides.RemoveRange(_dbContext.Rides);
        _dbContext.Drivers.RemoveRange(_dbContext.Drivers);
        _dbContext.Riders.RemoveRange(_dbContext.Riders);
        await _dbContext.SaveChangesAsync();

        await DbInitializer.InitializeAsync(_dbContext, _locationStore, _logger);

        var drivers = await _driverService.GetAllAsync();
        var riders = await _dbContext.Riders.ToListAsync();

        return Ok(new
        {
            Message = "Database re-seeded successfully",
            DriversCount = drivers.Count,
            RidersCount = riders.Count
        });
    }

    [HttpPost("driver-step")]
    public async Task<IActionResult> MoveDriverStep([FromBody] DriverStepRequest request, CancellationToken cancellationToken)
    {
        var driver = await _driverService.GetByIdAsync(request.DriverId, cancellationToken);
        var curLat = driver.CurrentLatitude;
        var curLon = driver.CurrentLongitude;

        // Linear interpolation step towards target
        var t = Math.Clamp(request.StepFraction, 0.01, 1.0);
        var newLat = curLat + ((request.TargetLatitude - curLat) * t);
        var newLon = curLon + ((request.TargetLongitude - curLon) * t);

        var updated = await _driverService.UpdateLocationAsync(
            request.DriverId,
            new UpdateLocationRequest(newLat, newLon, "En route"),
            cancellationToken);

        var distanceRemaining = GeoCalculator.CalculateDistanceKm(newLat, newLon, request.TargetLatitude, request.TargetLongitude);

        return Ok(new
        {
            Driver = updated,
            DistanceRemainingKm = distanceRemaining,
            Arrived = distanceRemaining < 0.05
        });
    }
}
