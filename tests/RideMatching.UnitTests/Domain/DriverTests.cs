using FluentAssertions;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;
using RideMatching.Domain.Exceptions;
using RideMatching.Domain.ValueObjects;
using Xunit;

namespace RideMatching.UnitTests.Domain;

public class DriverTests
{
    private static Driver CreateTestDriver()
    {
        var vehicle = new VehicleDetails("Toyota", "Camry", "ABC1234", "Black", VehicleType.Economy);
        return new Driver("John Doe", "john@example.com", "+1-555-1234", vehicle, 37.7749, -122.4194);
    }

    [Fact]
    public void NewDriver_ShouldHaveOfflineStatus_AndDefaultRating()
    {
        var driver = CreateTestDriver();

        driver.Status.Should().Be(DriverStatus.Offline);
        driver.Rating.Should().Be(5.0);
        driver.TotalRides.Should().Be(0);
        driver.CurrentRideId.Should().BeNull();
    }

    [Fact]
    public void GoOnline_ShouldSetStatusToAvailable()
    {
        var driver = CreateTestDriver();

        driver.GoOnline();

        driver.Status.Should().Be(DriverStatus.Available);
    }

    [Fact]
    public void GoOffline_ShouldSetStatusToOffline()
    {
        var driver = CreateTestDriver();
        driver.GoOnline();

        driver.GoOffline();

        driver.Status.Should().Be(DriverStatus.Offline);
    }

    [Fact]
    public void UpdateLocation_ShouldUpdateCoordinatesAndTimestamp()
    {
        var driver = CreateTestDriver();

        driver.UpdateLocation(37.7850, -122.4060, "Market St");

        driver.CurrentLatitude.Should().Be(37.7850);
        driver.CurrentLongitude.Should().Be(-122.4060);
        driver.CurrentAddress.Should().Be("Market St");
        driver.LastLocationUpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AssignRide_WhenAvailable_ShouldSetAssignedStatus()
    {
        var driver = CreateTestDriver();
        driver.GoOnline();
        var rideId = Guid.NewGuid();

        driver.AssignRide(rideId);

        driver.Status.Should().Be(DriverStatus.Assigned);
        driver.CurrentRideId.Should().Be(rideId);
    }

    [Fact]
    public void AssignRide_WhenOffline_ShouldThrowDriverUnavailableException()
    {
        var driver = CreateTestDriver();

        var action = () => driver.AssignRide(Guid.NewGuid());

        action.Should().Throw<DriverUnavailableException>();
    }

    [Fact]
    public void CompleteRide_ShouldUpdateRatingAndTotalRides()
    {
        var driver = CreateTestDriver();
        driver.GoOnline();
        driver.AssignRide(Guid.NewGuid());
        driver.StartRide();

        driver.CompleteRide(4.0);

        driver.TotalRides.Should().Be(1);
        driver.Rating.Should().Be(4.0);
        driver.Status.Should().Be(DriverStatus.Available);
        driver.CurrentRideId.Should().BeNull();
    }
}
