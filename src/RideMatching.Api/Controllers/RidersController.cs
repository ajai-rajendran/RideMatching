using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;

namespace RideMatching.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RidersController : ControllerBase
{
    private readonly IRiderService _riderService;
    private readonly IRideService _rideService;
    private readonly IValidator<RegisterRiderRequest> _registerValidator;

    public RidersController(
        IRiderService riderService,
        IRideService rideService,
        IValidator<RegisterRiderRequest> registerValidator)
    {
        _riderService = riderService;
        _rideService = rideService;
        _registerValidator = registerValidator;
    }

    [HttpPost]
    public async Task<RiderResponse> RegisterRider([FromBody] RegisterRiderRequest request, CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        return await _riderService.RegisterRiderAsync(request, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<RiderResponse> GetRiderById(Guid id, CancellationToken cancellationToken)
    {
        return await _riderService.GetByIdAsync(id, cancellationToken);
    }

    [HttpGet]
    public async Task<IReadOnlyList<RiderResponse>> GetAllRiders(CancellationToken cancellationToken)
    {
        return await _riderService.GetAllAsync(cancellationToken);
    }

    [HttpGet("{id:guid}/active-ride")]
    public async Task<RideResponse?> GetActiveRide(Guid id, CancellationToken cancellationToken)
    {
        return await _rideService.GetActiveRideForRiderAsync(id, cancellationToken);
    }
}
