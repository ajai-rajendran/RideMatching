using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RideMatching.Application.DTOs;
using RideMatching.Domain.Enums;
using Xunit;

namespace RideMatching.IntegrationTests;

public class RidesWorkflowApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RidesWorkflowApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EndToEnd_RideLifecycle_ShouldSucceed()
    {
        // 1. Register a new Driver
        var regDriverReq = new RegisterDriverRequest(
            "Test Driver Alex",
            $"alex_{Guid.NewGuid()}@rides.com",
            "+1-555-9988",
            "Toyota",
            "Camry",
            "TEST-999",
            "Blue",
            VehicleType.Economy,
            37.7749,
            -122.4194);

        var driverRes = await _client.PostAsJsonAsync("/api/drivers", regDriverReq);
        driverRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var driver = await driverRes.Content.ReadFromJsonAsync<DriverResponse>();
        driver.Should().NotBeNull();
        var driverId = driver!.Id;

        // 2. Set Driver Online
        var onlineRes = await _client.PostAsync($"/api/drivers/{driverId}/online", null);
        onlineRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var onlineDriver = await onlineRes.Content.ReadFromJsonAsync<DriverResponse>();
        onlineDriver!.Status.Should().Be("Available");

        // 3. Update Driver GPS Location
        var locReq = new UpdateLocationRequest(37.7750, -122.4190, "Market St & 8th");
        var locRes = await _client.PostAsJsonAsync($"/api/drivers/{driverId}/location", locReq);
        locRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Register Rider
        var regRiderReq = new RegisterRiderRequest(
            "Test Rider Emily",
            $"emily_{Guid.NewGuid()}@rider.com",
            "+1-555-7766");

        var riderRes = await _client.PostAsJsonAsync("/api/riders", regRiderReq);
        riderRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var rider = await riderRes.Content.ReadFromJsonAsync<RiderResponse>();
        rider.Should().NotBeNull();
        var riderId = rider!.Id;

        // 5. Request Ride
        var createRideReq = new CreateRideRequest(
            riderId,
            37.7749, -122.4194, "Civic Center Plaza",
            37.7879, -122.4075, "Union Square SF");

        var rideRes = await _client.PostAsJsonAsync("/api/rides", createRideReq);
        rideRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var ride = await rideRes.Content.ReadFromJsonAsync<RideResponse>();
        ride.Should().NotBeNull();
        var rideId = ride!.Id;
        ride.Status.Should().Be("Requested");
        ride.EstimatedFare.Should().BeGreaterThan(0);

        // 6. Driver Accepts Ride
        var acceptRes = await _client.PostAsJsonAsync($"/api/rides/{rideId}/accept", new { DriverId = driverId });
        acceptRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var acceptedRide = await acceptRes.Content.ReadFromJsonAsync<RideResponse>();
        acceptedRide!.Status.Should().Be("Accepted");
        acceptedRide.DriverId.Should().Be(driverId);

        // 7. Driver Starts Ride
        var startRes = await _client.PostAsJsonAsync($"/api/rides/{rideId}/start", new { DriverId = driverId });
        startRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var startedRide = await startRes.Content.ReadFromJsonAsync<RideResponse>();
        startedRide!.Status.Should().Be("Started");

        // 8. Driver Completes Ride
        var completeReq = new CompleteRideRequest(FinalFare: 14.50m, DriverRating: 5.0);
        var completeRes = await _client.PostAsJsonAsync($"/api/rides/{rideId}/complete?driverId={driverId}", completeReq);
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var completedRide = await completeRes.Content.ReadFromJsonAsync<RideResponse>();
        completedRide!.Status.Should().Be("Completed");
        completedRide.ActualFare.Should().Be(14.50m);

        // 9. Verify Driver is back to Available and has 1 completed ride
        var getDriverRes = await _client.GetAsync($"/api/drivers/{driverId}");
        var finalDriver = await getDriverRes.Content.ReadFromJsonAsync<DriverResponse>();
        finalDriver!.Status.Should().Be("Available");
        finalDriver.TotalRides.Should().Be(1);
    }
}
