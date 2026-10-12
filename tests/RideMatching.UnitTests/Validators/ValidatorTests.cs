using FluentAssertions;
using RideMatching.Application.DTOs;
using RideMatching.Application.Validators;
using RideMatching.Domain.Enums;
using Xunit;

namespace RideMatching.UnitTests.Validators;

public class ValidatorTests
{
    [Fact]
    public void RegisterDriverRequestValidator_WhenValid_ShouldPass()
    {
        var validator = new RegisterDriverRequestValidator();
        var request = new RegisterDriverRequest(
            "Driver One",
            "driver1@example.com",
            "+1-555-1234",
            "Honda",
            "Civic",
            "XYZ-999",
            "Blue",
            VehicleType.Economy,
            37.7749,
            -122.4194);

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterDriverRequestValidator_WhenInvalidEmail_ShouldFail()
    {
        var validator = new RegisterDriverRequestValidator();
        var request = new RegisterDriverRequest(
            "Driver One",
            "not-an-email",
            "+1-555-1234",
            "Honda",
            "Civic",
            "XYZ-999",
            "Blue",
            VehicleType.Economy);

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void CreateRideRequestValidator_WhenPickupAndDropoffSame_ShouldFail()
    {
        var validator = new CreateRideRequestValidator();
        var request = new CreateRideRequest(
            Guid.NewGuid(),
            37.7749, -122.4194, "Market St",
            37.7749, -122.4194, "Market St");

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("identical"));
    }
}
