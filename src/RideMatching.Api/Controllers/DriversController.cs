using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;

namespace RideMatching.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;
    private readonly IRideService _rideService;
    private readonly IValidator<RegisterDriverRequest> _registerValidator;
    private readonly IValidator<UpdateLocationRequest> _locationValidator;

    public DriversController(
        IDriverService driverService,
        IRideService rideService,
        IValidator<RegisterDriverRequest> registerValidator,
        IValidator<UpdateLocationRequest> locationValidator)
    {
        _driverService = driverService;
        _rideService = rideService;
        _registerValidator = registerValidator;
        _locationValidator = locationValidator;
    }

    [HttpPost]
    public async Task<DriverResponse> RegisterDriver([FromBody] RegisterDriverRequest request, CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        return await _driverService.RegisterDriverAsync(request, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<DriverResponse> GetDriverById(Guid id, CancellationToken cancellationToken)
    {
        return await _driverService.GetByIdAsync(id, cancellationToken);
    }

    [HttpGet]
    public async Task<IReadOnlyList<DriverResponse>> GetAllDrivers(CancellationToken cancellationToken)
    {
        return await _driverService.GetAllAsync(cancellationToken);
    }

    [HttpGet("available")]
    public async Task<IReadOnlyList<AvailableDriverDto>> GetAvailableDrivers(
        [FromQuery] double? lat,
        [FromQuery] double? lon,
        [FromQuery] double radiusKm = 15.0,
        CancellationToken cancellationToken = default)
    {
        var latitude = lat ?? 37.7749;
        var longitude = lon ?? -122.4194;
        return await _driverService.GetNearbyAvailableDriversAsync(latitude, longitude, radiusKm, cancellationToken);
    }

    [HttpPost("{id:guid}/online")]
    public async Task<DriverResponse> GoOnline(Guid id, CancellationToken cancellationToken)
    {
        return await _driverService.SetOnlineAsync(id, cancellationToken);
    }

    [HttpPost("{id:guid}/offline")]
    public async Task<DriverResponse> GoOffline(Guid id, CancellationToken cancellationToken)
    {
        return await _driverService.SetOfflineAsync(id, cancellationToken);
    }

    [HttpPost("{id:guid}/location")]
    public async Task<DriverResponse> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var validation = await _locationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        return await _driverService.UpdateLocationAsync(id, request, cancellationToken);
    }

    [HttpGet("{id:guid}/active-ride")]
    public async Task<RideResponse?> GetActiveRide(Guid id, CancellationToken cancellationToken)
    {
        return await _rideService.GetActiveRideForDriverAsync(id, cancellationToken);
    }
}
