using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;

namespace RideMatching.Infrastructure.Persistence.Configurations;

public class RideConfiguration : IEntityTypeConfiguration<Ride>
{
    public void Configure(EntityTypeBuilder<Ride> builder)
    {
        builder.ToTable("Rides");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RiderId);
        builder.HasIndex(r => r.DriverId);

        builder.Property(r => r.EstimatedFare)
            .HasPrecision(18, 2);

        builder.Property(r => r.ActualFare)
            .HasPrecision(18, 2);

        builder.Property(r => r.CancellationReason)
            .HasMaxLength(250);

        builder.Property(r => r.CancelledBy)
            .HasMaxLength(50);

        builder.OwnsOne(r => r.PickupLocation, pickup =>
        {
            pickup.Property(p => p.Latitude).HasColumnName("PickupLatitude").IsRequired();
            pickup.Property(p => p.Longitude).HasColumnName("PickupLongitude").IsRequired();
            pickup.Property(p => p.Address).HasColumnName("PickupAddress").HasMaxLength(250).IsRequired();
        });

        builder.OwnsOne(r => r.DropoffLocation, dropoff =>
        {
            dropoff.Property(p => p.Latitude).HasColumnName("DropoffLatitude").IsRequired();
            dropoff.Property(p => p.Longitude).HasColumnName("DropoffLongitude").IsRequired();
            dropoff.Property(p => p.Address).HasColumnName("DropoffAddress").HasMaxLength(250).IsRequired();
        });

        // Store rejected driver ids list as JSON
        builder.Property(r => r.RejectedDriverIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<Guid>()
                    : JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnName("RejectedDriverIds");

        builder.HasOne(r => r.Rider)
            .WithMany()
            .HasForeignKey(r => r.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Driver)
            .WithMany()
            .HasForeignKey(r => r.DriverId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
