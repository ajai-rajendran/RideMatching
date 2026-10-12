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

public class DriverService : IDriverService
{
    private readonly IDriverRepository _driverRepository;
    private readonly IDriverLocationStore _locationStore;
    private readonly IRideNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DriverService> _logger;

    public DriverService(
        IDriverRepository driverRepository,
        IDriverLocationStore locationStore,
        IRideNotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<DriverService> logger)
    {
        _driverRepository = driverRepository;
        _locationStore = locationStore;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DriverResponse> RegisterDriverAsync(RegisterDriverRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _driverRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            throw new DomainException($"A driver with email '{request.Email}' is already registered.");
        }

        var vehicle = new VehicleDetails(
            request.Make,
            request.Model,
            request.LicensePlate,
            request.Color,
            request.VehicleType);

        var driver = new Driver(
            request.Name,
            request.Email,
            request.PhoneNumber,
            vehicle,
            request.InitialLatitude ?? 0,
            request.InitialLongitude ?? 0);

        if (request.InitialLatitude.HasValue && request.InitialLongitude.HasValue)
        {
            driver.UpdateLocation(request.InitialLatitude.Value, request.InitialLongitude.Value);
        }

        await _driverRepository.AddAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Driver registered successfully with ID: {DriverId}", driver.Id);
        return MapToDto(driver);
    }

    public async Task<DriverResponse> SetOnlineAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Driver), driverId);

        driver.GoOnline();
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _locationStore.UpdateLocationAsync(driver.Id, driver.CurrentLatitude, driver.CurrentLongitude, cancellationToken);
        await _notificationService.NotifyDriverStatusChangedAsync(driver.Id, driver.Status.ToString());

        _logger.LogInformation("Driver {DriverId} went ONLINE at ({Lat}, {Lon})", driver.Id, driver.CurrentLatitude, driver.CurrentLongitude);
        return MapToDto(driver);
    }

    public async Task<DriverResponse> SetOfflineAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Driver), driverId);

        driver.GoOffline();
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _locationStore.RemoveLocationAsync(driver.Id, cancellationToken);
        await _notificationService.NotifyDriverStatusChangedAsync(driver.Id, driver.Status.ToString());

        _logger.LogInformation("Driver {DriverId} went OFFLINE", driver.Id);
        return MapToDto(driver);
    }

    public async Task<DriverResponse> UpdateLocationAsync(Guid driverId, UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Driver), driverId);

        driver.UpdateLocation(request.Latitude, request.Longitude, request.Address);
        await _driverRepository.UpdateAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (driver.Status == DriverStatus.Available)
        {
            await _locationStore.UpdateLocationAsync(driver.Id, request.Latitude, request.Longitude, cancellationToken);
        }

        await _notificationService.NotifyDriverLocationUpdatedAsync(
            driver.Id,
            request.Latitude,
            request.Longitude,
            request.Address,
            driver.LastLocationUpdatedAt ?? DateTime.UtcNow);

        return MapToDto(driver);
    }

    public async Task<DriverResponse> GetByIdAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Driver), driverId);

        return MapToDto(driver);
    }

    public async Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var drivers = await _driverRepository.GetAllAsync(cancellationToken);
        return drivers.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<AvailableDriverDto>> GetNearbyAvailableDriversAsync(double latitude, double longitude, double radiusKm = 15.0, CancellationToken cancellationToken = default)
    {
        var geoDrivers = await _locationStore.FindNearbyDriversAsync(latitude, longitude, radiusKm, 20, cancellationToken);
        var result = new List<AvailableDriverDto>();

        foreach (var gd in geoDrivers)
        {
            var driver = await _driverRepository.GetByIdAsync(gd.DriverId, cancellationToken);
            if (driver != null && driver.Status == DriverStatus.Available)
            {
                var dist = gd.DistanceKm > 0 ? gd.DistanceKm : GeoCalculator.CalculateDistanceKm(latitude, longitude, driver.CurrentLatitude, driver.CurrentLongitude);
                result.Add(new AvailableDriverDto(
                    driver.Id,
                    driver.Name,
                    driver.CurrentLatitude,
                    driver.CurrentLongitude,
                    dist,
                    driver.Rating,
                    new VehicleDetailsDto(
                        driver.Vehicle.Make,
                        driver.Vehicle.Model,
                        driver.Vehicle.LicensePlate,
                        driver.Vehicle.Color,
                        driver.Vehicle.Type.ToString())));
            }
        }

        return result.OrderBy(x => x.DistanceKm).ToList();
    }

    private static DriverResponse MapToDto(Driver driver) => new(
        driver.Id,
        driver.Name,
        driver.Email,
        driver.PhoneNumber,
        new VehicleDetailsDto(
            driver.Vehicle.Make,
            driver.Vehicle.Model,
            driver.Vehicle.LicensePlate,
            driver.Vehicle.Color,
            driver.Vehicle.Type.ToString()),
        driver.Status.ToString(),
        driver.CurrentLatitude,
        driver.CurrentLongitude,
        driver.CurrentAddress,
        driver.Rating,
        driver.TotalRides,
        driver.CurrentRideId,
        driver.LastLocationUpdatedAt,
        driver.CreatedAt);
}
