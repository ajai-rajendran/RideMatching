using Microsoft.EntityFrameworkCore;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;
using RideMatching.Domain.Repositories;
using RideMatching.Infrastructure.Persistence;

namespace RideMatching.Infrastructure.Persistence.Repositories;

public class RideRepository : IRideRepository
{
    private readonly RideMatchingDbContext _context;

    public RideRepository(RideMatchingDbContext context)
    {
        _context = context;
    }

    public async Task<Ride?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Include(r => r.Rider)
            .Include(r => r.Driver)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Ride?> GetActiveRideForRiderAsync(Guid riderId, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Include(r => r.Rider)
            .Include(r => r.Driver)
            .Where(r => r.RiderId == riderId &&
                        r.Status != RideStatus.Completed &&
                        r.Status != RideStatus.Cancelled)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Ride?> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Include(r => r.Rider)
            .Include(r => r.Driver)
            .Where(r => (r.DriverId == driverId || r.CandidateDriverId == driverId) &&
                        r.Status != RideStatus.Completed &&
                        r.Status != RideStatus.Cancelled)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ride>> GetPendingRidesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Include(r => r.Rider)
            .Where(r => r.Status == RideStatus.Requested || r.Status == RideStatus.Matching)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ride>> GetAllAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Include(r => r.Rider)
            .Include(r => r.Driver)
            .OrderByDescending(r => r.RequestedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ride>> GetByRiderIdAsync(Guid riderId, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Include(r => r.Driver)
            .Where(r => r.RiderId == riderId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ride>> GetByDriverIdAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Include(r => r.Rider)
            .Where(r => r.DriverId == driverId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Ride ride, CancellationToken cancellationToken = default)
    {
        await _context.Rides.AddAsync(ride, cancellationToken);
    }

    public Task UpdateAsync(Ride ride, CancellationToken cancellationToken = default)
    {
        _context.Rides.Update(ride);
        return Task.CompletedTask;
    }
}
