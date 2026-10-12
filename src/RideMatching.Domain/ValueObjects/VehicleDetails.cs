using RideMatching.Domain.Enums;

namespace RideMatching.Domain.ValueObjects;

public record VehicleDetails
{
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string LicensePlate { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public VehicleType Type { get; init; } = VehicleType.Economy;

    public VehicleDetails() { }

    public VehicleDetails(string make, string model, string licensePlate, string color, VehicleType type = VehicleType.Economy)
    {
        Make = make;
        Model = model;
        LicensePlate = licensePlate;
        Color = color;
        Type = type;
    }
}
