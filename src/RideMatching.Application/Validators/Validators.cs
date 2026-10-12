using FluentValidation;
using RideMatching.Application.DTOs;

namespace RideMatching.Application.Validators;

public class RegisterDriverRequestValidator : AbstractValidator<RegisterDriverRequest>
{
    public RegisterDriverRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Driver name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MinimumLength(7).MaximumLength(20);

        RuleFor(x => x.Make)
            .NotEmpty().WithMessage("Vehicle make is required.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Vehicle model is required.");

        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithMessage("License plate is required.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Vehicle color is required.");

        When(x => x.InitialLatitude.HasValue, () =>
        {
            RuleFor(x => x.InitialLatitude!.Value)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");
        });

        When(x => x.InitialLongitude.HasValue, () =>
        {
            RuleFor(x => x.InitialLongitude!.Value)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
        });
    }
}

public class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
    }
}

public class RegisterRiderRequestValidator : AbstractValidator<RegisterRiderRequest>
{
    public RegisterRiderRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rider name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MinimumLength(7).MaximumLength(20);
    }
}

public class CreateRideRequestValidator : AbstractValidator<CreateRideRequest>
{
    public CreateRideRequestValidator()
    {
        RuleFor(x => x.RiderId)
            .NotEmpty().WithMessage("RiderId is required.");

        RuleFor(x => x.PickupLatitude)
            .InclusiveBetween(-90, 90).WithMessage("Pickup latitude must be between -90 and 90.");

        RuleFor(x => x.PickupLongitude)
            .InclusiveBetween(-180, 180).WithMessage("Pickup longitude must be between -180 and 180.");

        RuleFor(x => x.DropoffLatitude)
            .InclusiveBetween(-90, 90).WithMessage("Dropoff latitude must be between -90 and 90.");

        RuleFor(x => x.DropoffLongitude)
            .InclusiveBetween(-180, 180).WithMessage("Dropoff longitude must be between -180 and 180.");

        RuleFor(x => x.PickupAddress)
            .NotEmpty().WithMessage("Pickup address is required.");

        RuleFor(x => x.DropoffAddress)
            .NotEmpty().WithMessage("Dropoff address is required.");

        RuleFor(x => x)
            .Must(x => Math.Abs(x.PickupLatitude - x.DropoffLatitude) > 0.00001 ||
                       Math.Abs(x.PickupLongitude - x.DropoffLongitude) > 0.00001)
            .WithMessage("Pickup and Dropoff locations cannot be identical.");
    }
}

public class CancelRideRequestValidator : AbstractValidator<CancelRideRequest>
{
    public CancelRideRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MaximumLength(250);
    }
}

public class EstimateRideDtoValidator : AbstractValidator<EstimateRideDto>
{
    public EstimateRideDtoValidator()
    {
        RuleFor(x => x.PickupLatitude)
            .InclusiveBetween(-90, 90).WithMessage("Pickup latitude must be between -90 and 90.");

        RuleFor(x => x.PickupLongitude)
            .InclusiveBetween(-180, 180).WithMessage("Pickup longitude must be between -180 and 180.");

        RuleFor(x => x.DropoffLatitude)
            .InclusiveBetween(-90, 90).WithMessage("Dropoff latitude must be between -90 and 90.");

        RuleFor(x => x.DropoffLongitude)
            .InclusiveBetween(-180, 180).WithMessage("Dropoff longitude must be between -180 and 180.");
    }
}
