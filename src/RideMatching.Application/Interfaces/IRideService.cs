using RideMatching.Application.DTOs;

namespace RideMatching.Application.Interfaces;

public interface IRideService
{
    Task<RideResponse> RequestRideAsync(CreateRideRequest request, CancellationToken cancellationToken = default);
    Task<RideResponse> GetByIdAsync(Guid rideId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RideResponse>> GetAllAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<RideResponse?> GetActiveRideForRiderAsync(Guid riderId, CancellationToken cancellationToken = default);
    Task<RideResponse?> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<RideResponse> AcceptRideAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken = default);
    Task<RideResponse> RejectRideAsync(Guid rideId, Guid driverId, RejectRideRequest? request = null, CancellationToken cancellationToken = default);
    Task<RideResponse> StartRideAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken = default);
    Task<RideResponse> CompleteRideAsync(Guid rideId, Guid driverId, CompleteRideRequest? request = null, CancellationToken cancellationToken = default);
    Task<RideResponse> CancelRideAsync(Guid rideId, CancelRideRequest request, CancellationToken cancellationToken = default);
    Task<RideEstimateResponse> EstimateRideAsync(EstimateRideDto request, CancellationToken cancellationToken = default);
    Task<bool> ProcessMatchingAttemptAsync(Guid rideId, int attempt, CancellationToken cancellationToken = default);
}
