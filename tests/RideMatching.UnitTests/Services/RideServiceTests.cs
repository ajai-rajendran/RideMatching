using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;
using RideMatching.Application.Services;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;
using RideMatching.Domain.Exceptions;
using RideMatching.Domain.Repositories;
using RideMatching.Domain.ValueObjects;
using Xunit;

namespace RideMatching.UnitTests.Services;

public class RideServiceTests
{
    private readonly Mock<IRideRepository> _rideRepoMock = new();
    private readonly Mock<IRiderRepository> _riderRepoMock = new();
    private readonly Mock<IDriverRepository> _driverRepoMock = new();
    private readonly Mock<IDriverLocationStore> _locationStoreMock = new();
    private readonly Mock<IRideMatchingChannel> _channelMock = new();
    private readonly Mock<IRideNotificationService> _notifMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<RideService>> _loggerMock = new();

    private RideService CreateService()
    {
        return new RideService(
            _rideRepoMock.Object,
            _riderRepoMock.Object,
            _driverRepoMock.Object,
            _locationStoreMock.Object,
            _channelMock.Object,
            _notifMock.Object,
            _uowMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RequestRide_WhenValid_ShouldCreateRide_AndEnqueueMatching()
    {
        var rider = new Rider("Alice", "alice@example.com", "12345678");
        var riderId = rider.Id;

        _riderRepoMock.Setup(r => r.GetByIdAsync(riderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rider);

        _rideRepoMock.Setup(r => r.GetActiveRideForRiderAsync(riderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ride?)null);

        var service = CreateService();

        var request = new CreateRideRequest(
            riderId,
            37.7749, -122.4194, "Civic Center",
            37.7879, -122.4075, "Union Square");

        var response = await service.RequestRideAsync(request);

        response.Should().NotBeNull();
        response.Status.Should().Be("Requested");
        response.RiderId.Should().Be(riderId);

        _rideRepoMock.Verify(r => r.AddAsync(It.IsAny<Ride>(), It.IsAny<CancellationToken>()), Times.Once);
        _channelMock.Verify(c => c.EnqueueAsync(It.Is<RideMatchingTask>(t => t.RideId == response.Id), It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.NotifyRideRequestedAsync(It.IsAny<RideResponse>()), Times.Once);
    }

    [Fact]
    public async Task RequestRide_WhenRiderAlreadyHasActiveRide_ShouldThrowDomainException()
    {
        var rider = new Rider("Alice", "alice@example.com", "12345678");
        var riderId = rider.Id;
        var activeRide = new Ride(riderId, new Location(37, -122), new Location(38, -122), 5, 10, 15);

        _riderRepoMock.Setup(r => r.GetByIdAsync(riderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rider);

        _rideRepoMock.Setup(r => r.GetActiveRideForRiderAsync(riderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeRide);

        var service = CreateService();

        var request = new CreateRideRequest(
            riderId,
            37.7749, -122.4194, "Civic Center",
            37.7879, -122.4075, "Union Square");

        var action = () => service.RequestRideAsync(request);

        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("*already has an active ride*");
    }

    [Fact]
    public async Task AcceptRide_WhenValid_ShouldSetRideAccepted_AndAssignDriver()
    {
        var driver = new Driver("Bob", "bob@example.com", "1234567", new VehicleDetails("Tesla", "3", "ABC", "Red"), 37.77, -122.41);
        driver.GoOnline();
        var driverId = driver.Id;

        var ride = new Ride(Guid.NewGuid(), new Location(37.77, -122.41), new Location(37.78, -122.40), 2, 5, 10);
        ride.MarkMatching();
        ride.MatchWithDriver(driverId);
        var rideId = ride.Id;

        _rideRepoMock.Setup(r => r.GetByIdAsync(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        _driverRepoMock.Setup(d => d.GetByIdAsync(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(driver);

        var service = CreateService();

        var response = await service.AcceptRideAsync(rideId, driverId);

        response.Status.Should().Be("Accepted");
        driver.Status.Should().Be(DriverStatus.Assigned);
        driver.CurrentRideId.Should().Be(ride.Id);

        _locationStoreMock.Verify(l => l.RemoveLocationAsync(driverId, It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.NotifyRideAcceptedAsync(It.IsAny<RideResponse>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMatchingAttempt_WhenDriverNearby_ShouldMatchAndNotify()
    {
        var driver = new Driver("Charlie", "charlie@rides.com", "98765432", new VehicleDetails("Toyota", "Prius", "123", "Silver"), 37.7750, -122.4190);
        driver.GoOnline();
        var driverId = driver.Id;

        var ride = new Ride(Guid.NewGuid(), new Location(37.7749, -122.4194), new Location(37.7879, -122.4075), 2.0, 5.0, 10.0m);

        _rideRepoMock.Setup(r => r.GetByIdAsync(ride.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        _locationStoreMock.Setup(l => l.FindNearbyDriversAsync(ride.PickupLocation.Latitude, ride.PickupLocation.Longitude, It.IsAny<double>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DriverGeoLocation> { new(driverId, 37.7750, -122.4190, 0.2) });

        _driverRepoMock.Setup(d => d.GetByIdAsync(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(driver);

        var service = CreateService();

        var matched = await service.ProcessMatchingAttemptAsync(ride.Id, 1);

        matched.Should().BeTrue();
        ride.Status.Should().Be(RideStatus.Matched);
        ride.CandidateDriverId.Should().Be(driverId);
        _notifMock.Verify(n => n.NotifyRideMatchedAsync(ride.Id, driverId, ride.RiderId, It.IsAny<RideResponse>()), Times.Once);
    }
}
