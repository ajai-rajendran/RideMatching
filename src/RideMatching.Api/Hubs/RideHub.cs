using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace RideMatching.Api.Hubs;

public class RideHub : Hub
{
    private readonly ILogger<RideHub> _logger;

    public RideHub(ILogger<RideHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRideGroup(string rideId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ride_{rideId}");
        _logger.LogDebug("Connection {ConnectionId} joined group ride_{RideId}", Context.ConnectionId, rideId);
    }

    public async Task LeaveRideGroup(string rideId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride_{rideId}");
        _logger.LogDebug("Connection {ConnectionId} left group ride_{RideId}", Context.ConnectionId, rideId);
    }

    public async Task RegisterDriver(string driverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"driver_{driverId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "drivers");
        _logger.LogInformation("Driver {DriverId} registered on connection {ConnectionId}", driverId, Context.ConnectionId);
    }

    public async Task RegisterRider(string riderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"rider_{riderId}");
        _logger.LogInformation("Rider {RiderId} registered on connection {ConnectionId}", riderId, Context.ConnectionId);
    }
}
