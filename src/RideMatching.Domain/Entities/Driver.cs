using RideMatching.Domain.Enums;
using RideMatching.Domain.Exceptions;
using RideMatching.Domain.ValueObjects;

namespace RideMatching.Domain.Entities;

public class Driver
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public VehicleDetails Vehicle { get; private set; } = new();
    public DriverStatus Status { get; private set; } = DriverStatus.Offline;
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public string? CurrentAddress { get; private set; }
    public DateTime? LastLocationUpdatedAt { get; private set; }
    public double Rating { get; private set; } = 5.0;
    public int TotalRides { get; private set; }
    public Guid? CurrentRideId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Driver() { }

    public Driver(string name, string email, string phoneNumber, VehicleDetails vehicle, double initialLat = 0, double initialLon = 0)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        Vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
        Status = DriverStatus.Offline;
        CurrentLatitude = initialLat;
        CurrentLongitude = initialLon;
        Rating = 5.0;
        TotalRides = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void GoOnline()
    {
        if (Status == DriverStatus.InRide)
            throw new InvalidRideStateException("Cannot go online while actively in a ride.");
        
        Status = DriverStatus.Available;
    }

    public void GoOffline()
    {
        if (Status == DriverStatus.InRide)
            throw new InvalidRideStateException("Cannot go offline while in a ride. Please finish or cancel the ride first.");
        
        Status = DriverStatus.Offline;
        CurrentRideId = null;
    }

    public void UpdateLocation(double latitude, double longitude, string? address = null)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        if (!string.IsNullOrWhiteSpace(address))
        {
            CurrentAddress = address;
        }
        LastLocationUpdatedAt = DateTime.UtcNow;
    }

    public void AssignRide(Guid rideId)
    {
        if (Status != DriverStatus.Available)
            throw new DriverUnavailableException($"Driver {Id} is not available for assignment (Status: {Status}).");

        Status = DriverStatus.Assigned;
        CurrentRideId = rideId;
    }

    public void StartRide()
    {
        if (Status != DriverStatus.Assigned)
            throw new InvalidRideStateException($"Cannot start ride. Driver status is {Status}, expected Assigned.");

        Status = DriverStatus.InRide;
    }

    public void CompleteRide(double tripRating)
    {
        Status = DriverStatus.Available;
        CurrentRideId = null;
        TotalRides++;
        Rating = Math.Round(((Rating * (TotalRides - 1)) + Math.Clamp(tripRating, 1.0, 5.0)) / TotalRides, 2);
    }

    public void Release()
    {
        Status = DriverStatus.Available;
        CurrentRideId = null;
    }
}
