using Microsoft.Extensions.Logging;
using RideMatching.Application.Common;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;
using RideMatching.Domain.Exceptions;
using RideMatching.Domain.Repositories;
using RideMatching.Domain.ValueObjects;

namespace RideMatching.Application.Services;

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepository;
    private readonly IRiderRepository _riderRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IDriverLocationStore _locationStore;
    private readonly IRideMatchingChannel _matchingChannel;
    private readonly IRideNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RideService> _logger;

    public RideService(
        IRideRepository rideRepository,
        IRiderRepository riderRepository,
        IDriverRepository driverRepository,
        IDriverLocationStore locationStore,
        IRideMatchingChannel matchingChannel,
        IRideNotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<RideService> logger)
    {
        _rideRepository = rideRepository;
        _riderRepository = riderRepository;
        _driverRepository = driverRepository;
        _locationStore = locationStore;
        _matchingChannel = matchingChannel;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RideResponse> RequestRideAsync(CreateRideRequest request, CancellationToken cancellationToken = default)
    {
        var rider = await _riderRepository.GetByIdAsync(request.RiderId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Rider), request.RiderId);

        var existingActiveRide = await _rideRepository.GetActiveRideForRiderAsync(request.RiderId, cancellationToken);
        if (existingActiveRide != null)
        {
            throw new DomainException($"Rider already has an active ride (ID: {existingActiveRide.Id}, Status: {existingActiveRide.Status}).");
        }

        var distanceKm = GeoCalculator.CalculateDistanceKm(
            request.PickupLatitude, request.PickupLongitude,
            request.DropoffLatitude, request.DropoffLongitude);

        var durationMinutes = GeoCalculator.EstimateDurationMinutes(distanceKm);
        var estimatedFare = Ride.CalculateEstimatedFare(distanceKm, durationMinutes);

        var pickup = new Location(request.PickupLatitude, request.PickupLongitude, request.PickupAddress);
        var dropoff = new Location(request.DropoffLatitude, request.DropoffLongitude, request.DropoffAddress);

        var ride = new Ride(
            rider.Id,
            pickup,
            dropoff,
            distanceKm,
            durationMinutes,
            estimatedFare);

        await _rideRepository.AddAsync(ride, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ride requested successfully with ID: {RideId} for Rider: {RiderId}", ride.Id, rider.Id);

        // Queue ride matching task for asynchronous background worker
        await _matchingChannel.EnqueueAsync(new RideMatchingTask(ride.Id, 1), cancellationToken);

        var response = await BuildRideResponseAsync(ride, cancellationToken);
        await _notificationService.NotifyRideRequestedAsync(response);

        return response;
    }

    public async Task<bool> ProcessMatchingAttemptAsync(Guid rideId, int attempt, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken);
        if (ride == null || ride.Status is RideStatus.Cancelled or RideStatus.Completed or RideStatus.Accepted or RideStatus.Started)
        {
            _logger.LogInformation("Ride {RideId} is in status {Status}; skipping matching attempt {Attempt}.", rideId, ride?.Status, attempt);
            return false;
        }

        ride.MarkMatching();
        await _rideRepository.UpdateAsync(ride, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyRideMatchingAsync(ride.Id, ride.RiderId, attempt);

        // Dynamic search radius expansion based on attempt
        double radiusKm = attempt switch
        {
            1 => 8.0,
            2 => 15.0,
            _ => 30.0
        };

        var nearbyDrivers = await _locationStore.FindNearbyDriversAsync(
            ride.PickupLocation.Latitude,
            ride.PickupLocation.Longitude,
            radiusKm,
            20,
            cancellationToken);

        var rejectedIds = ride.RejectedDriverIds.ToHashSet();

        Driver? chosenDriver = null;
        double bestDistance = double.MaxValue;

        foreach (var candidateGeo in nearbyDrivers)
        {
            if (rejectedIds.Contains(candidateGeo.DriverId))
                continue;

            var driver = await _driverRepository.GetByIdAsync(candidateGeo.DriverId, cancellationToken);
            if (driver != null && driver.Status == DriverStatus.Available)
            {
                var dist = candidateGeo.DistanceKm > 0
                    ? candidateGeo.DistanceKm
                    : GeoCalculator.CalculateDistanceKm(
                        ride.PickupLocation.Latitude, ride.PickupLocation.Longitude,
                        driver.CurrentLatitude, driver.CurrentLongitude);

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    chosenDriver = driver;
                }
            }
        }

        // If no driver in redis cache, fallback search available drivers in database
        if (chosenDriver == null)
        {
            var availableDbDrivers = await _driverRepository.GetAvailableDriversAsync(cancellationToken);
            foreach (var driver in availableDbDrivers)
            {
                if (rejectedIds.Contains(driver.Id))
                    continue;

                var dist = GeoCalculator.CalculateDistanceKm(
                    ride.PickupLocation.Latitude, ride.PickupLocation.Longitude,
                    driver.CurrentLatitude, driver.CurrentLongitude);

                if (dist <= radiusKm && dist < bestDistance)
                {
                    bestDistance = dist;
                    chosenDriver = driver;
                }
            }
        }

        if (chosenDriver != null)
        {
            ride.MatchWithDriver(chosenDriver.Id);
            await _rideRepository.UpdateAsync(ride, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Ride {RideId} matched with Driver {DriverId} (Distance: {Distance:F2} km)",
                ride.Id, chosenDriver.Id, bestDistance);

            var response = await BuildRideResponseAsync(ride, cancellationToken);
            await _notificationService.NotifyRideMatchedAsync(ride.Id, chosenDriver.Id, ride.RiderId, response);
            return true;
        }

        _logger.LogWarning("No candidate driver found for Ride {RideId} in attempt {Attempt} within radius {Radius} km.",
            ride.Id, attempt, radiusKm);

        return false;
    }

    public async Task<RideResponse> AcceptRideAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Ride), rideId);

        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Driver), driverId);

        if (driver.Status != DriverStatus.Available && driver.Status != DriverStatus.Assigned)
        {
            throw new DriverUnavailableException($"Driver {driverId} is {driver.Status} and cannot accept ride.");
        }

        ride.Accept(driverId);
        driver.AssignRide(ride.Id);

        await _rideRepository.UpdateAsync(ride, cancellationToken);
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Remove from available driver location pool so no other rides match this driver
        await _locationStore.RemoveLocationAsync(driver.Id, cancellationToken);

        _logger.LogInformation("Driver {DriverId} accepted Ride {RideId}", driverId, rideId);

        var response = await BuildRideResponseAsync(ride, cancellationToken);
        await _notificationService.NotifyRideAcceptedAsync(response);
        await _notificationService.NotifyDriverStatusChangedAsync(driver.Id, driver.Status.ToString());

        return response;
    }

    public async Task<RideResponse> RejectRideAsync(Guid rideId, Guid driverId, RejectRideRequest? request = null, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Ride), rideId);

        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);
        if (driver != null)
        {
            driver.Release();
            await _driverRepository.UpdateAsync(driver, cancellationToken);
            await _locationStore.UpdateLocationAsync(driver.Id, driver.CurrentLatitude, driver.CurrentLongitude, cancellationToken);
            await _notificationService.NotifyDriverStatusChangedAsync(driver.Id, driver.Status.ToString());
        }

        ride.Reject(driverId, request?.Reason);
        await _rideRepository.UpdateAsync(ride, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Driver {DriverId} rejected Ride {RideId} (Reason: {Reason})", driverId, rideId, request?.Reason ?? "None");

        await _notificationService.NotifyRideRejectedAsync(rideId, driverId, request?.Reason);

        // Re-enqueue for matching with the next candidate
        await _matchingChannel.EnqueueAsync(new RideMatchingTask(ride.Id, ride.MatchAttempts + 1), cancellationToken);

        return await BuildRideResponseAsync(ride, cancellationToken);
    }

    public async Task<RideResponse> StartRideAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Ride), rideId);

        if (ride.DriverId != driverId)
            throw new DomainException($"Driver {driverId} is not assigned to ride {rideId}.");

        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Driver), driverId);

        ride.Start();
        driver.StartRide();

        await _rideRepository.UpdateAsync(ride, cancellationToken);
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ride {RideId} started by Driver {DriverId}", rideId, driverId);

        var response = await BuildRideResponseAsync(ride, cancellationToken);
        await _notificationService.NotifyRideStartedAsync(response);
        await _notificationService.NotifyDriverStatusChangedAsync(driver.Id, driver.Status.ToString());

        return response;
    }

    public async Task<RideResponse> CompleteRideAsync(Guid rideId, Guid driverId, CompleteRideRequest? request = null, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Ride), rideId);

        if (ride.DriverId != driverId)
            throw new DomainException($"Driver {driverId} is not assigned to ride {rideId}.");

        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Driver), driverId);

        var rider = await _riderRepository.GetByIdAsync(ride.RiderId, cancellationToken);

        var finalFare = request?.FinalFare ?? ride.EstimatedFare;
        var tripRating = request?.DriverRating ?? 5.0;

        ride.Complete(finalFare);
        driver.CompleteRide(tripRating);

        if (rider != null)
        {
            rider.RecordCompletedRide(tripRating);
            await _riderRepository.UpdateAsync(rider, cancellationToken);
        }

        await _rideRepository.UpdateAsync(ride, cancellationToken);
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Put driver back to available in location store
        await _locationStore.UpdateLocationAsync(driver.Id, driver.CurrentLatitude, driver.CurrentLongitude, cancellationToken);

        _logger.LogInformation("Ride {RideId} completed with fare ${Fare:F2}", rideId, finalFare);

        var response = await BuildRideResponseAsync(ride, cancellationToken);
        await _notificationService.NotifyRideCompletedAsync(response);
        await _notificationService.NotifyDriverStatusChangedAsync(driver.Id, driver.Status.ToString());

        return response;
    }

    public async Task<RideResponse> CancelRideAsync(Guid rideId, CancelRideRequest request, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Ride), rideId);

        var cancelledBy = request.CancelledBy ?? "User";
        ride.Cancel(request.Reason, cancelledBy);

        if (ride.DriverId.HasValue)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.DriverId.Value, cancellationToken);
            if (driver != null)
            {
                driver.Release();
                await _driverRepository.UpdateAsync(driver, cancellationToken);
                await _locationStore.UpdateLocationAsync(driver.Id, driver.CurrentLatitude, driver.CurrentLongitude, cancellationToken);
                await _notificationService.NotifyDriverStatusChangedAsync(driver.Id, driver.Status.ToString());
            }
        }

        await _rideRepository.UpdateAsync(ride, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ride {RideId} cancelled by {CancelledBy}. Reason: {Reason}", rideId, cancelledBy, request.Reason);

        var response = await BuildRideResponseAsync(ride, cancellationToken);
        await _notificationService.NotifyRideCancelledAsync(response, request.Reason);

        return response;
    }

    public async Task<RideResponse> GetByIdAsync(Guid rideId, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetByIdAsync(rideId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Ride), rideId);

        return await BuildRideResponseAsync(ride, cancellationToken);
    }

    public async Task<IReadOnlyList<RideResponse>> GetAllAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var rides = await _rideRepository.GetAllAsync(limit, cancellationToken);
        var list = new List<RideResponse>();
        foreach (var r in rides)
        {
            list.Add(await BuildRideResponseAsync(r, cancellationToken));
        }
        return list;
    }

    public async Task<RideResponse?> GetActiveRideForRiderAsync(Guid riderId, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetActiveRideForRiderAsync(riderId, cancellationToken);
        return ride != null ? await BuildRideResponseAsync(ride, cancellationToken) : null;
    }

    public async Task<RideResponse?> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var ride = await _rideRepository.GetActiveRideForDriverAsync(driverId, cancellationToken);
        return ride != null ? await BuildRideResponseAsync(ride, cancellationToken) : null;
    }

    public Task<RideEstimateResponse> EstimateRideAsync(EstimateRideDto request, CancellationToken cancellationToken = default)
    {
        var distanceKm = GeoCalculator.CalculateDistanceKm(
            request.PickupLatitude, request.PickupLongitude,
            request.DropoffLatitude, request.DropoffLongitude);

        var durationMinutes = GeoCalculator.EstimateDurationMinutes(distanceKm);
        var fare = Ride.CalculateEstimatedFare(distanceKm, durationMinutes);

        return Task.FromResult(new RideEstimateResponse(
            distanceKm,
            durationMinutes,
            fare,
            3.50m,
            1.75m,
            0.35m));
    }

    private async Task<RideResponse> BuildRideResponseAsync(Ride ride, CancellationToken cancellationToken)
    {
        RiderResponse? riderDto = null;
        if (ride.Rider != null)
        {
            riderDto = new RiderResponse(ride.Rider.Id, ride.Rider.Name, ride.Rider.Email, ride.Rider.PhoneNumber, ride.Rider.Rating, ride.Rider.TotalRides, ride.Rider.CreatedAt);
        }
        else
        {
            var r = await _riderRepository.GetByIdAsync(ride.RiderId, cancellationToken);
            if (r != null)
                riderDto = new RiderResponse(r.Id, r.Name, r.Email, r.PhoneNumber, r.Rating, r.TotalRides, r.CreatedAt);
        }

        DriverResponse? driverDto = null;
        if (ride.DriverId.HasValue)
        {
            var d = ride.Driver ?? await _driverRepository.GetByIdAsync(ride.DriverId.Value, cancellationToken);
            if (d != null)
            {
                driverDto = new DriverResponse(
                    d.Id,
                    d.Name,
                    d.Email,
                    d.PhoneNumber,
                    new VehicleDetailsDto(d.Vehicle.Make, d.Vehicle.Model, d.Vehicle.LicensePlate, d.Vehicle.Color, d.Vehicle.Type.ToString()),
                    d.Status.ToString(),
                    d.CurrentLatitude,
                    d.CurrentLongitude,
                    d.CurrentAddress,
                    d.Rating,
                    d.TotalRides,
                    d.CurrentRideId,
                    d.LastLocationUpdatedAt,
                    d.CreatedAt);
            }
        }

        return new RideResponse(
            ride.Id,
            ride.RiderId,
            riderDto,
            ride.DriverId,
            driverDto,
            ride.CandidateDriverId,
            ride.Status.ToString(),
            new LocationDto(ride.PickupLocation.Latitude, ride.PickupLocation.Longitude, ride.PickupLocation.Address),
            new LocationDto(ride.DropoffLocation.Latitude, ride.DropoffLocation.Longitude, ride.DropoffLocation.Address),
            ride.EstimatedFare,
            ride.ActualFare,
            ride.DistanceKm,
            ride.EstimatedDurationMinutes,
            ride.MatchAttempts,
            ride.RequestedAt,
            ride.MatchedAt,
            ride.AcceptedAt,
            ride.StartedAt,
            ride.CompletedAt,
            ride.CancelledAt,
            ride.CancellationReason,
            ride.CancelledBy);
    }
}
