using FluentAssertions;
using RideMatching.Application.Common;
using Xunit;

namespace RideMatching.UnitTests.Common;

public class GeoCalculatorTests
{
    [Fact]
    public void CalculateDistanceKm_SameLocation_ShouldReturnZero()
    {
        var dist = GeoCalculator.CalculateDistanceKm(37.7749, -122.4194, 37.7749, -122.4194);
        dist.Should().Be(0.0);
    }

    [Fact]
    public void CalculateDistanceKm_KnownPoints_ShouldReturnExpectedDistance()
    {
        // SF Civic Center (37.7749, -122.4194) to SF Union Square (37.7879, -122.4075)
        var dist = GeoCalculator.CalculateDistanceKm(37.7749, -122.4194, 37.7879, -122.4075);
        dist.Should().BeInRange(1.5, 2.2);
    }

    [Fact]
    public void CalculateBearing_NorthDirection_ShouldBeApproximatelyZero()
    {
        var bearing = GeoCalculator.CalculateBearing(37.0, -122.0, 38.0, -122.0);
        bearing.Should().BeApproximately(0.0, 1.0);
    }

    [Fact]
    public void EstimateDurationMinutes_ShouldBeProportionalToDistance()
    {
        var duration = GeoCalculator.EstimateDurationMinutes(15.0); // 15 km @ 30 km/h = 30 min + 2 min buffer = 32 min
        duration.Should().Be(32.0);
    }
}
