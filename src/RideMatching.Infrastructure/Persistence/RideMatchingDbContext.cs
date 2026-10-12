using Microsoft.EntityFrameworkCore;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Repositories;

namespace RideMatching.Infrastructure.Persistence;

public class RideMatchingDbContext : DbContext, IUnitOfWork
{
    public RideMatchingDbContext(DbContextOptions<RideMatchingDbContext> options) : base(options) { }

    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Rider> Riders => Set<Rider>();
    public DbSet<Ride> Rides => Set<Ride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RideMatchingDbContext).Assembly);
    }
}
