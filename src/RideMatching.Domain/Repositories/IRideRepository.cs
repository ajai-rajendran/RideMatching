using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;

namespace RideMatching.Domain.Repositories;

public interface IRideRepository
{
    Task<Ride?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Ride?> GetActiveRideForRiderAsync(Guid riderId, CancellationToken cancellationToken = default);
    Task<Ride?> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ride>> GetPendingRidesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ride>> GetAllAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ride>> GetByRiderIdAsync(Guid riderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ride>> GetByDriverIdAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task AddAsync(Ride ride, CancellationToken cancellationToken = default);
    Task UpdateAsync(Ride ride, CancellationToken cancellationToken = default);
}
