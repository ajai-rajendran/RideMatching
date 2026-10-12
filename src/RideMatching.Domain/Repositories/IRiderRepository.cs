using RideMatching.Domain.Entities;

namespace RideMatching.Domain.Repositories;

public interface IRiderRepository
{
    Task<Rider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Rider?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rider>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Rider rider, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rider rider, CancellationToken cancellationToken = default);
}
