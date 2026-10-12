namespace RideMatching.Application.DTOs;

public record RegisterRiderRequest(
    string Name,
    string Email,
    string PhoneNumber);

public record RiderResponse(
    Guid Id,
    string Name,
    string Email,
    string PhoneNumber,
    double Rating,
    int TotalRides,
    DateTime CreatedAt);
