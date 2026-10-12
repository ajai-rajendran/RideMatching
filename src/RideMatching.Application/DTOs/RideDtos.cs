namespace RideMatching.Application.DTOs;

public record LocationDto(
    double Latitude,
    double Longitude,
    string Address);

public record CreateRideRequest(
    Guid RiderId,
    double PickupLatitude,
    double PickupLongitude,
    string PickupAddress,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffAddress);

public record CancelRideRequest(
    string Reason,
    string? CancelledBy = null);

public record RejectRideRequest(
    string? Reason = null);

public record CompleteRideRequest(
    decimal? FinalFare = null,
    double? DriverRating = 5.0);

public record EstimateRideDto(
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude);

public record RideEstimateResponse(
    double DistanceKm,
    double EstimatedDurationMinutes,
    decimal EstimatedFare,
    decimal BaseFare,
    decimal PerKmRate,
    decimal PerMinuteRate);

public record RideResponse(
    Guid Id,
    Guid RiderId,
    RiderResponse? Rider,
    Guid? DriverId,
    DriverResponse? Driver,
    Guid? CandidateDriverId,
    string Status,
    LocationDto PickupLocation,
    LocationDto DropoffLocation,
    decimal EstimatedFare,
    decimal? ActualFare,
    double DistanceKm,
    double EstimatedDurationMinutes,
    int MatchAttempts,
    DateTime RequestedAt,
    DateTime? MatchedAt,
    DateTime? AcceptedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    string? CancelledBy);
