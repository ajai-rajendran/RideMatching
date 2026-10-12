using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RideMatching.Application.Common;
using RideMatching.Application.Interfaces;
using StackExchange.Redis;

namespace RideMatching.Infrastructure.Services;

public class RedisDriverLocationStore : IDriverLocationStore
{
    private const string GeoKey = "drivers:available:geo";
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisDriverLocationStore> _logger;
    private readonly ConcurrentDictionary<Guid, (double Latitude, double Longitude, DateTime UpdatedAt)> _inMemoryFallback = new();

    public RedisDriverLocationStore(
        ILogger<RedisDriverLocationStore> logger,
        IConnectionMultiplexer? redis = null)
    {
        _logger = logger;
        _redis = redis;
    }

    public async Task UpdateLocationAsync(Guid driverId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        _inMemoryFallback[driverId] = (latitude, longitude, DateTime.UtcNow);

        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.GeoAddAsync(GeoKey, longitude, latitude, driverId.ToString());
                _logger.LogDebug("Updated Redis GEO location for driver {DriverId}: ({Lat}, {Lon})", driverId, latitude, longitude);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update driver location in Redis. Using in-memory fallback.");
            }
        }
    }

    public async Task RemoveLocationAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        _inMemoryFallback.TryRemove(driverId, out _);

        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.GeoRemoveAsync(GeoKey, driverId.ToString());
                _logger.LogDebug("Removed driver {DriverId} from Redis GEO set", driverId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove driver location from Redis.");
            }
        }
    }

    public async Task<IReadOnlyList<DriverGeoLocation>> FindNearbyDriversAsync(double latitude, double longitude, double radiusKm, int limit = 20, CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var results = await db.GeoRadiusAsync(
                    GeoKey,
                    longitude,
                    latitude,
                    radiusKm,
                    GeoUnit.Kilometers,
                    count: limit,
                    order: Order.Ascending,
                    options: GeoRadiusOptions.WithDistance | GeoRadiusOptions.WithCoordinates);

                if (results != null && results.Length > 0)
                {
                    var list = new List<DriverGeoLocation>();
                    foreach (var res in results)
                    {
                        if (res.Member.HasValue && Guid.TryParse(res.Member.ToString(), out var driverId))
                        {
                            var dist = res.Distance.HasValue ? Math.Round(res.Distance.Value, 2) : 0.0;
                            var posLat = res.Position.HasValue ? res.Position.Value.Latitude : latitude;
                            var posLon = res.Position.HasValue ? res.Position.Value.Longitude : longitude;
                            list.Add(new DriverGeoLocation(driverId, posLat, posLon, dist));
                        }
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query Redis GeoRadius. Falling back to in-memory spatial calculation.");
            }
        }

        // In-memory spatial fallback
        var nearby = new List<DriverGeoLocation>();
        foreach (var (driverId, (lat, lon, _)) in _inMemoryFallback)
        {
            var dist = GeoCalculator.CalculateDistanceKm(latitude, longitude, lat, lon);
            if (dist <= radiusKm)
            {
                nearby.Add(new DriverGeoLocation(driverId, lat, lon, dist));
            }
        }

        return nearby.OrderBy(x => x.DistanceKm).Take(limit).ToList();
    }

    public async Task<(double Latitude, double Longitude)?> GetLocationAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var pos = await db.GeoPositionAsync(GeoKey, driverId.ToString());
                if (pos.HasValue)
                {
                    return (pos.Value.Latitude, pos.Value.Longitude);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get driver location from Redis.");
            }
        }

        if (_inMemoryFallback.TryGetValue(driverId, out var val))
        {
            return (val.Latitude, val.Longitude);
        }

        return null;
    }
}
