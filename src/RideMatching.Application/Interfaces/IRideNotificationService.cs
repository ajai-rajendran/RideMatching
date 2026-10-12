using RideMatching.Application.DTOs;

namespace RideMatching.Application.Interfaces;

public interface IRideNotificationService
{
    Task NotifyDriverLocationUpdatedAsync(Guid driverId, double latitude, double longitude, string? address, DateTime timestamp);
    Task NotifyDriverStatusChangedAsync(Guid driverId, string status);
    Task NotifyRideRequestedAsync(RideResponse ride);
    Task NotifyRideMatchingAsync(Guid rideId, Guid riderId, int attempt);
    Task NotifyRideMatchedAsync(Guid rideId, Guid driverId, Guid riderId, RideResponse ride);
    Task NotifyRideAcceptedAsync(RideResponse ride);
    Task NotifyRideRejectedAsync(Guid rideId, Guid driverId, string? reason);
    Task NotifyRideStartedAsync(RideResponse ride);
    Task NotifyRideCompletedAsync(RideResponse ride);
    Task NotifyRideCancelledAsync(RideResponse ride, string reason);
}
