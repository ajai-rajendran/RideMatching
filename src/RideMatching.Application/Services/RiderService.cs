using Microsoft.Extensions.Logging;
using RideMatching.Application.DTOs;
using RideMatching.Application.Interfaces;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Exceptions;
using RideMatching.Domain.Repositories;

namespace RideMatching.Application.Services;

public class RiderService : IRiderService
{
    private readonly IRiderRepository _riderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RiderService> _logger;

    public RiderService(
        IRiderRepository riderRepository,
        IUnitOfWork unitOfWork,
        ILogger<RiderService> logger)
    {
        _riderRepository = riderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RiderResponse> RegisterRiderAsync(RegisterRiderRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _riderRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            throw new DomainException($"A rider with email '{request.Email}' is already registered.");
        }

        var rider = new Rider(request.Name, request.Email, request.PhoneNumber);
        await _riderRepository.AddAsync(rider, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rider registered successfully with ID: {RiderId}", rider.Id);
        return MapToDto(rider);
    }

    public async Task<RiderResponse> GetByIdAsync(Guid riderId, CancellationToken cancellationToken = default)
    {
        var rider = await _riderRepository.GetByIdAsync(riderId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Rider), riderId);

        return MapToDto(rider);
    }

    public async Task<IReadOnlyList<RiderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var riders = await _riderRepository.GetAllAsync(cancellationToken);
        return riders.Select(MapToDto).ToList();
    }

    private static RiderResponse MapToDto(Rider rider) => new(
        rider.Id,
        rider.Name,
        rider.Email,
        rider.PhoneNumber,
        rider.Rating,
        rider.TotalRides,
        rider.CreatedAt);
}
