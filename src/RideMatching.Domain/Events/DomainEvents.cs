namespace RideMatching.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record DriverLocationUpdatedEvent(
    Guid DriverId,
    double Latitude,
    double Longitude,
    string? Address,
    DateTime Timestamp) : DomainEvent;

public record DriverStatusChangedEvent(
    Guid DriverId,
    string Status) : DomainEvent;

public record RideRequestedEvent(
    Guid RideId,
    Guid RiderId,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    decimal EstimatedFare) : DomainEvent;

public record RideMatchingEvent(
    Guid RideId,
    int Attempt) : DomainEvent;

public record RideMatchedEvent(
    Guid RideId,
    Guid DriverId,
    double DriverLatitude,
    double DriverLongitude,
    double DistanceToPickupKm) : DomainEvent;

public record RideAcceptedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId) : DomainEvent;

public record RideRejectedEvent(
    Guid RideId,
    Guid DriverId,
    string? Reason) : DomainEvent;

public record RideStartedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId) : DomainEvent;

public record RideCompletedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId,
    decimal FinalFare,
    double DistanceKm) : DomainEvent;

public record RideCancelledEvent(
    Guid RideId,
    string Reason,
    string CancelledBy) : DomainEvent;
