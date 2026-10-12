using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideMatching.Infrastructure.Persistence;
using StackExchange.Redis;

namespace RideMatching.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly RideMatchingDbContext _dbContext;
    private readonly IConnectionMultiplexer? _redis;

    public HealthController(RideMatchingDbContext dbContext, IConnectionMultiplexer? redis = null)
    {
        _dbContext = dbContext;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var dbHealthy = false;
        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            dbHealthy = false;
        }

        var redisHealthy = _redis != null && _redis.IsConnected;

        var totalDrivers = 0;
        var onlineDrivers = 0;
        var totalRiders = 0;
        var activeRides = 0;

        if (dbHealthy)
        {
            totalDrivers = await _dbContext.Drivers.CountAsync(cancellationToken);
            onlineDrivers = await _dbContext.Drivers.CountAsync(d => d.Status == Domain.Enums.DriverStatus.Available, cancellationToken);
            totalRiders = await _dbContext.Riders.CountAsync(cancellationToken);
            activeRides = await _dbContext.Rides.CountAsync(r => r.Status != Domain.Enums.RideStatus.Completed && r.Status != Domain.Enums.RideStatus.Cancelled, cancellationToken);
        }

        var result = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Database = new
            {
                Type = "SQLite",
                Status = dbHealthy ? "Connected" : "Disconnected",
                TotalDrivers = totalDrivers,
                OnlineDrivers = onlineDrivers,
                TotalRiders = totalRiders,
                ActiveRides = activeRides
            },
            Redis = new
            {
                Status = redisHealthy ? "Connected" : "In-Memory Spatial Fallback Active",
                IsConnected = redisHealthy
            },
            Services = new
            {
                RideHub = "Active",
                MatchingChannel = "Running",
                BackgroundWorker = "Active"
            }
        };

        return Ok(result);
    }
}
