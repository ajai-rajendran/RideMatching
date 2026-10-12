using RideMatching.Domain.Enums;

namespace RideMatching.Application.DTOs;

public record RegisterDriverRequest(
    string Name,
    string Email,
    string PhoneNumber,
    string Make,
    string Model,
    string LicensePlate,
    string Color,
    VehicleType VehicleType = VehicleType.Economy,
    double? InitialLatitude = null,
    double? InitialLongitude = null);

public record UpdateLocationRequest(
    double Latitude,
    double Longitude,
    string? Address = null);

public record VehicleDetailsDto(
    string Make,
    string Model,
    string LicensePlate,
    string Color,
    string Type);

public record DriverResponse(
    Guid Id,
    string Name,
    string Email,
    string PhoneNumber,
    VehicleDetailsDto Vehicle,
    string Status,
    double CurrentLatitude,
    double CurrentLongitude,
    string? CurrentAddress,
    double Rating,
    int TotalRides,
    Guid? CurrentRideId,
    DateTime? LastLocationUpdatedAt,
    DateTime CreatedAt);

public record AvailableDriverDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double DistanceKm,
    double Rating,
    VehicleDetailsDto Vehicle);
