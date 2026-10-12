using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RideMatching.Api.Hubs;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;

namespace RideMatching.Api.Services;

public class SignalRRideNotificationService : IRideNotificationService
{
    private readonly IHubContext<RideHub> _hubContext;
    private readonly ILogger<SignalRRideNotificationService> _logger;

    public SignalRRideNotificationService(
        IHubContext<RideHub> hubContext,
        ILogger<SignalRRideNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyDriverLocationUpdatedAsync(Guid driverId, double latitude, double longitude, string? address, DateTime timestamp)
    {
        var payload = new
        {
            DriverId = driverId,
            Latitude = latitude,
            Longitude = longitude,
            Address = address,
            Timestamp = timestamp
        };

        // Broadcast to all clients watching the map or specific driver channels
        await _hubContext.Clients.All.SendAsync("DriverLocationUpdated", payload);
    }

    public async Task NotifyDriverStatusChangedAsync(Guid driverId, string status)
    {
        var payload = new
        {
            DriverId = driverId,
            Status = status
        };

        await _hubContext.Clients.All.SendAsync("DriverStatusChanged", payload);
    }

    public async Task NotifyRideRequestedAsync(RideResponse ride)
    {
        await _hubContext.Clients.All.SendAsync("RideRequested", ride);
        await _hubContext.Clients.Group($"rider_{ride.RiderId}").SendAsync("RideRequested", ride);
        _logger.LogInformation("Notified RideRequested for Ride {RideId}", ride.Id);
    }

    public async Task NotifyRideMatchingAsync(Guid rideId, Guid riderId, int attempt)
    {
        var payload = new { RideId = rideId, RiderId = riderId, Attempt = attempt };
        await _hubContext.Clients.All.SendAsync("RideMatching", payload);
        await _hubContext.Clients.Group($"ride_{rideId}").SendAsync("RideMatching", payload);
        await _hubContext.Clients.Group($"rider_{riderId}").SendAsync("RideMatching", payload);
    }

    public async Task NotifyRideMatchedAsync(Guid rideId, Guid driverId, Guid riderId, RideResponse ride)
    {
        var payload = new
        {
            RideId = rideId,
            DriverId = driverId,
            RiderId = riderId,
            Ride = ride
        };

        // Notify specific driver channel, rider channel, and ride group
        await _hubContext.Clients.All.SendAsync("RideMatched", payload);
        await _hubContext.Clients.Group($"driver_{driverId}").SendAsync("RideOffer", payload);
        await _hubContext.Clients.Group($"rider_{riderId}").SendAsync("RideMatched", payload);
        await _hubContext.Clients.Group($"ride_{rideId}").SendAsync("RideMatched", payload);

        _logger.LogInformation("Notified RideMatched for Ride {RideId} to Driver {DriverId}", rideId, driverId);
    }

    public async Task NotifyRideAcceptedAsync(RideResponse ride)
    {
        await _hubContext.Clients.All.SendAsync("RideAccepted", ride);
        if (ride.DriverId.HasValue)
        {
            await _hubContext.Clients.Group($"driver_{ride.DriverId.Value}").SendAsync("RideAccepted", ride);
        }
        await _hubContext.Clients.Group($"rider_{ride.RiderId}").SendAsync("RideAccepted", ride);
        await _hubContext.Clients.Group($"ride_{ride.Id}").SendAsync("RideAccepted", ride);
        _logger.LogInformation("Notified RideAccepted for Ride {RideId}", ride.Id);
    }

    public async Task NotifyRideRejectedAsync(Guid rideId, Guid driverId, string? reason)
    {
        var payload = new { RideId = rideId, DriverId = driverId, Reason = reason };
        await _hubContext.Clients.All.SendAsync("RideRejected", payload);
        await _hubContext.Clients.Group($"driver_{driverId}").SendAsync("RideRejected", payload);
        await _hubContext.Clients.Group($"ride_{rideId}").SendAsync("RideRejected", payload);
    }

    public async Task NotifyRideStartedAsync(RideResponse ride)
    {
        await _hubContext.Clients.All.SendAsync("RideStarted", ride);
        if (ride.DriverId.HasValue)
        {
            await _hubContext.Clients.Group($"driver_{ride.DriverId.Value}").SendAsync("RideStarted", ride);
        }
        await _hubContext.Clients.Group($"rider_{ride.RiderId}").SendAsync("RideStarted", ride);
        await _hubContext.Clients.Group($"ride_{ride.Id}").SendAsync("RideStarted", ride);
        _logger.LogInformation("Notified RideStarted for Ride {RideId}", ride.Id);
    }

    public async Task NotifyRideCompletedAsync(RideResponse ride)
    {
        await _hubContext.Clients.All.SendAsync("RideCompleted", ride);
        if (ride.DriverId.HasValue)
        {
            await _hubContext.Clients.Group($"driver_{ride.DriverId.Value}").SendAsync("RideCompleted", ride);
        }
        await _hubContext.Clients.Group($"rider_{ride.RiderId}").SendAsync("RideCompleted", ride);
        await _hubContext.Clients.Group($"ride_{ride.Id}").SendAsync("RideCompleted", ride);
        _logger.LogInformation("Notified RideCompleted for Ride {RideId}", ride.Id);
    }

    public async Task NotifyRideCancelledAsync(RideResponse ride, string reason)
    {
        var payload = new { Ride = ride, Reason = reason };
        await _hubContext.Clients.All.SendAsync("RideCancelled", payload);
        if (ride.DriverId.HasValue)
        {
            await _hubContext.Clients.Group($"driver_{ride.DriverId.Value}").SendAsync("RideCancelled", payload);
        }
        await _hubContext.Clients.Group($"rider_{ride.RiderId}").SendAsync("RideCancelled", payload);
        await _hubContext.Clients.Group($"ride_{ride.Id}").SendAsync("RideCancelled", payload);
        _logger.LogInformation("Notified RideCancelled for Ride {RideId}", ride.Id);
    }
}
