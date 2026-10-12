using Microsoft.EntityFrameworkCore;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Repositories;
using RideMatching.Infrastructure.Persistence;

namespace RideMatching.Infrastructure.Persistence.Repositories;

public class RiderRepository : IRiderRepository
{
    private readonly RideMatchingDbContext _context;

    public RiderRepository(RideMatchingDbContext context)
    {
        _context = context;
    }

    public async Task<Rider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Riders.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Rider?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Riders.FirstOrDefaultAsync(r => r.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Rider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Riders.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Rider rider, CancellationToken cancellationToken = default)
    {
        await _context.Riders.AddAsync(rider, cancellationToken);
    }

    public Task UpdateAsync(Rider rider, CancellationToken cancellationToken = default)
    {
        _context.Riders.Update(rider);
        return Task.CompletedTask;
    }
}
