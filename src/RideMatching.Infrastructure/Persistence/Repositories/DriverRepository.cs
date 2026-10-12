using Microsoft.EntityFrameworkCore;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;
using RideMatching.Domain.Repositories;
using RideMatching.Infrastructure.Persistence;

namespace RideMatching.Infrastructure.Persistence.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly RideMatchingDbContext _context;

    public DriverRepository(RideMatchingDbContext context)
    {
        _context = context;
    }

    public async Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Drivers.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Driver?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Drivers.FirstOrDefaultAsync(d => d.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyList<Driver>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Drivers.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Driver>> GetAvailableDriversAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Drivers
            .Where(d => d.Status == DriverStatus.Available)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        await _context.Drivers.AddAsync(driver, cancellationToken);
    }

    public Task UpdateAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        _context.Drivers.Update(driver);
        return Task.CompletedTask;
    }
}
