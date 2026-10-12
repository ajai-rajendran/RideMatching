using RideMatching.Application.DTOs;

namespace RideMatching.Application.Interfaces;

public record DriverGeoLocation(Guid DriverId, double Latitude, double Longitude, double DistanceKm);

public interface IDriverLocationStore
{
    Task UpdateLocationAsync(Guid driverId, double latitude, double longitude, CancellationToken cancellationToken = default);
    Task RemoveLocationAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriverGeoLocation>> FindNearbyDriversAsync(double latitude, double longitude, double radiusKm, int limit = 20, CancellationToken cancellationToken = default);
    Task<(double Latitude, double Longitude)?> GetLocationAsync(Guid driverId, CancellationToken cancellationToken = default);
}
