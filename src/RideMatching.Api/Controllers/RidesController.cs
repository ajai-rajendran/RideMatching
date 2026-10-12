using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;

namespace RideMatching.Api.Controllers;

public record DriverActionRequest(Guid DriverId);

[ApiController]
[Route("api/[controller]")]
public class RidesController : ControllerBase
{
    private readonly IRideService _rideService;
    private readonly IValidator<CreateRideRequest> _createRideValidator;
    private readonly IValidator<CancelRideRequest> _cancelRideValidator;
    private readonly IValidator<EstimateRideDto> _estimateValidator;

    public RidesController(
        IRideService rideService,
        IValidator<CreateRideRequest> createRideValidator,
        IValidator<CancelRideRequest> cancelRideValidator,
        IValidator<EstimateRideDto> estimateValidator)
    {
        _rideService = rideService;
        _createRideValidator = createRideValidator;
        _cancelRideValidator = cancelRideValidator;
        _estimateValidator = estimateValidator;
    }

    [HttpPost]
    public async Task<RideResponse> RequestRide([FromBody] CreateRideRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createRideValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        return await _rideService.RequestRideAsync(request, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<RideResponse> GetRideById(Guid id, CancellationToken cancellationToken)
    {
        return await _rideService.GetByIdAsync(id, cancellationToken);
    }

    [HttpGet]
    public async Task<IReadOnlyList<RideResponse>> GetAllRides([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _rideService.GetAllAsync(limit, cancellationToken);
    }

    [HttpPost("estimate")]
    public async Task<RideEstimateResponse> EstimateRide([FromBody] EstimateRideDto request, CancellationToken cancellationToken)
    {
        var validation = await _estimateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        return await _rideService.EstimateRideAsync(request, cancellationToken);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<RideResponse> AcceptRide(Guid id, [FromBody] DriverActionRequest request, CancellationToken cancellationToken)
    {
        return await _rideService.AcceptRideAsync(id, request.DriverId, cancellationToken);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<RideResponse> RejectRide(Guid id, [FromBody] DriverActionRequest request, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        return await _rideService.RejectRideAsync(id, request.DriverId, new RejectRideRequest(reason), cancellationToken);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<RideResponse> StartRide(Guid id, [FromBody] DriverActionRequest request, CancellationToken cancellationToken)
    {
        return await _rideService.StartRideAsync(id, request.DriverId, cancellationToken);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<RideResponse> CompleteRide(Guid id, [FromBody] CompleteRideRequest request, [FromQuery] Guid driverId, CancellationToken cancellationToken)
    {
        return await _rideService.CompleteRideAsync(id, driverId, request, cancellationToken);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<RideResponse> CancelRide(Guid id, [FromBody] CancelRideRequest request, CancellationToken cancellationToken)
    {
        var validation = await _cancelRideValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        return await _rideService.CancelRideAsync(id, request, cancellationToken);
    }
}
