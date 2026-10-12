using RideMatching.Application.DTOs;

namespace RideMatching.Application.Interfaces;

public interface IDriverService
{
    Task<DriverResponse> RegisterDriverAsync(RegisterDriverRequest request, CancellationToken cancellationToken = default);
    Task<DriverResponse> SetOnlineAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<DriverResponse> SetOfflineAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<DriverResponse> UpdateLocationAsync(Guid driverId, UpdateLocationRequest request, CancellationToken cancellationToken = default);
    Task<DriverResponse> GetByIdAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AvailableDriverDto>> GetNearbyAvailableDriversAsync(double latitude, double longitude, double radiusKm = 15.0, CancellationToken cancellationToken = default);
}
