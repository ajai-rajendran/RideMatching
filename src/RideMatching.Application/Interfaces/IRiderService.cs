using RideMatching.Application.DTOs;

namespace RideMatching.Application.Interfaces;

public interface IRiderService
{
    Task<RiderResponse> RegisterRiderAsync(RegisterRiderRequest request, CancellationToken cancellationToken = default);
    Task<RiderResponse> GetByIdAsync(Guid riderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RiderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
