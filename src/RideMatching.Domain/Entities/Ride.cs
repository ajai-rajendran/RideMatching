using RideMatching.Domain.Enums;
using RideMatching.Domain.Exceptions;
using RideMatching.Domain.ValueObjects;

namespace RideMatching.Domain.Entities;

public class Ride
{
    private readonly List<Guid> _rejectedDriverIds = [];

    public Guid Id { get; private set; }
    public Guid RiderId { get; private set; }
    public Rider? Rider { get; private set; }
    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public Guid? CandidateDriverId { get; private set; }
    public RideStatus Status { get; private set; } = RideStatus.Requested;
    
    public Location PickupLocation { get; private set; } = new();
    public Location DropoffLocation { get; private set; } = new();

    public decimal EstimatedFare { get; private set; }
    public decimal? ActualFare { get; private set; }
    public double DistanceKm { get; private set; }
    public double EstimatedDurationMinutes { get; private set; }
    public int MatchAttempts { get; private set; }

    public IReadOnlyCollection<Guid> RejectedDriverIds => _rejectedDriverIds.AsReadOnly();

    public DateTime RequestedAt { get; private set; }
    public DateTime? MatchedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancelledBy { get; private set; }

    private Ride() { }

    public Ride(
        Guid riderId,
        Location pickupLocation,
        Location dropoffLocation,
        double distanceKm,
        double estimatedDurationMinutes,
        decimal estimatedFare)
    {
        Id = Guid.NewGuid();
        RiderId = riderId;
        PickupLocation = pickupLocation ?? throw new ArgumentNullException(nameof(pickupLocation));
        DropoffLocation = dropoffLocation ?? throw new ArgumentNullException(nameof(dropoffLocation));
        DistanceKm = Math.Max(0.1, distanceKm);
        EstimatedDurationMinutes = Math.Max(1, estimatedDurationMinutes);
        EstimatedFare = estimatedFare > 0 ? estimatedFare : CalculateEstimatedFare(DistanceKm, EstimatedDurationMinutes);
        Status = RideStatus.Requested;
        RequestedAt = DateTime.UtcNow;
    }

    public static decimal CalculateEstimatedFare(double distanceKm, double durationMinutes, decimal baseFare = 3.50m, decimal perKmRate = 1.75m, decimal perMinuteRate = 0.35m)
    {
        var fare = baseFare + ((decimal)distanceKm * perKmRate) + ((decimal)durationMinutes * perMinuteRate);
        return Math.Round(Math.Max(fare, 5.00m), 2);
    }

    public void MarkMatching()
    {
        if (Status is not (RideStatus.Requested or RideStatus.Matching or RideStatus.Matched))
            throw new InvalidRideStateException($"Cannot set ride {Id} to matching from status {Status}.");

        Status = RideStatus.Matching;
        MatchAttempts++;
        CandidateDriverId = null;
    }

    public void MatchWithDriver(Guid driverId)
    {
        if (Status != RideStatus.Matching && Status != RideStatus.Requested)
            throw new InvalidRideStateException($"Cannot match ride {Id} in current status {Status}.");

        CandidateDriverId = driverId;
        Status = RideStatus.Matched;
        MatchedAt = DateTime.UtcNow;
    }

    public void Accept(Guid driverId)
    {
        if (Status != RideStatus.Matched && Status != RideStatus.Matching)
            throw new InvalidRideStateException($"Ride {Id} cannot be accepted in status {Status}.");

        if (CandidateDriverId.HasValue && CandidateDriverId.Value != driverId)
            throw new DomainException($"Driver {driverId} was not offered ride {Id}.");

        DriverId = driverId;
        CandidateDriverId = null;
        Status = RideStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
    }

    public void Reject(Guid driverId, string? reason = null)
    {
        if (!_rejectedDriverIds.Contains(driverId))
        {
            _rejectedDriverIds.Add(driverId);
        }

        if (CandidateDriverId == driverId)
        {
            CandidateDriverId = null;
        }

        Status = RideStatus.Matching;
    }

    public void Start()
    {
        if (Status != RideStatus.Accepted)
            throw new InvalidRideStateException($"Ride {Id} cannot be started because it is not in Accepted status (Current: {Status}).");

        Status = RideStatus.Started;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(decimal? finalFare = null)
    {
        if (Status != RideStatus.Started)
            throw new InvalidRideStateException($"Ride {Id} cannot be completed because it is not in Started status (Current: {Status}).");

        Status = RideStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ActualFare = finalFare ?? EstimatedFare;
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status is RideStatus.Completed or RideStatus.Cancelled)
            throw new InvalidRideStateException($"Ride {Id} is already {Status} and cannot be cancelled.");

        Status = RideStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        CancelledBy = cancelledBy;
        CandidateDriverId = null;
    }

    public void SetRejectedDrivers(IEnumerable<Guid> driverIds)
    {
        _rejectedDriverIds.Clear();
        _rejectedDriverIds.AddRange(driverIds);
    }
}
