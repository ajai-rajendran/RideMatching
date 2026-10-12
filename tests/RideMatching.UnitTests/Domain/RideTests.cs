using FluentAssertions;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;
using RideMatching.Domain.Exceptions;
using RideMatching.Domain.ValueObjects;
using Xunit;

namespace RideMatching.UnitTests.Domain;

public class RideTests
{
    private static Ride CreateTestRide()
    {
        var pickup = new Location(37.7749, -122.4194, "Civic Center");
        var dropoff = new Location(37.7879, -122.4075, "Union Square");
        return new Ride(Guid.NewGuid(), pickup, dropoff, 2.5, 8.0, 12.50m);
    }

    [Fact]
    public void NewRide_ShouldHaveRequestedStatus()
    {
        var ride = CreateTestRide();

        ride.Status.Should().Be(RideStatus.Requested);
        ride.EstimatedFare.Should().Be(12.50m);
        ride.DriverId.Should().BeNull();
        ride.CandidateDriverId.Should().BeNull();
    }

    [Fact]
    public void CalculateEstimatedFare_ShouldApplyBaseAndPerKmRates()
    {
        var fare = Ride.CalculateEstimatedFare(10.0, 20.0);
        // Base: 3.50 + (10 * 1.75 = 17.50) + (20 * 0.35 = 7.00) = 28.00
        fare.Should().Be(28.00m);
    }

    [Fact]
    public void FullLifecycle_Requested_To_Completed_ShouldTransitionProperly()
    {
        var ride = CreateTestRide();
        var driverId = Guid.NewGuid();

        ride.MarkMatching();
        ride.Status.Should().Be(RideStatus.Matching);
        ride.MatchAttempts.Should().Be(1);

        ride.MatchWithDriver(driverId);
        ride.Status.Should().Be(RideStatus.Matched);
        ride.CandidateDriverId.Should().Be(driverId);

        ride.Accept(driverId);
        ride.Status.Should().Be(RideStatus.Accepted);
        ride.DriverId.Should().Be(driverId);
        ride.CandidateDriverId.Should().BeNull();

        ride.Start();
        ride.Status.Should().Be(RideStatus.Started);

        ride.Complete(15.00m);
        ride.Status.Should().Be(RideStatus.Completed);
        ride.ActualFare.Should().Be(15.00m);
        ride.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_ShouldAddDriverToRejectedList_AndResetCandidate()
    {
        var ride = CreateTestRide();
        var driverId = Guid.NewGuid();

        ride.MarkMatching();
        ride.MatchWithDriver(driverId);
        ride.Reject(driverId, "Too far");

        ride.Status.Should().Be(RideStatus.Matching);
        ride.CandidateDriverId.Should().BeNull();
        ride.RejectedDriverIds.Should().Contain(driverId);
    }

    [Fact]
    public void Cancel_WhenAccepted_ShouldSetCancelledStatus()
    {
        var ride = CreateTestRide();
        var driverId = Guid.NewGuid();

        ride.MarkMatching();
        ride.MatchWithDriver(driverId);
        ride.Accept(driverId);

        ride.Cancel("Changed mind", "Rider");

        ride.Status.Should().Be(RideStatus.Cancelled);
        ride.CancellationReason.Should().Be("Changed mind");
        ride.CancelledBy.Should().Be("Rider");
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrowInvalidRideStateException()
    {
        var ride = CreateTestRide();
        var driverId = Guid.NewGuid();

        ride.MarkMatching();
        ride.MatchWithDriver(driverId);
        ride.Accept(driverId);
        ride.Start();
        ride.Complete();

        var action = () => ride.Cancel("Too late", "Rider");

        action.Should().Throw<InvalidRideStateException>();
    }
}
