using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideMatching.Domain.Entities;
using RideMatching.Domain.Enums;

namespace RideMatching.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(d => d.Email).IsUnique();

        builder.Property(d => d.PhoneNumber)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(d => d.Status);

        builder.Property(d => d.CurrentAddress)
            .HasMaxLength(250);

        builder.OwnsOne(d => d.Vehicle, vehicle =>
        {
            vehicle.Property(v => v.Make).HasColumnName("VehicleMake").HasMaxLength(50).IsRequired();
            vehicle.Property(v => v.Model).HasColumnName("VehicleModel").HasMaxLength(50).IsRequired();
            vehicle.Property(v => v.LicensePlate).HasColumnName("VehicleLicensePlate").HasMaxLength(25).IsRequired();
            vehicle.Property(v => v.Color).HasColumnName("VehicleColor").HasMaxLength(30).IsRequired();
            vehicle.Property(v => v.Type).HasColumnName("VehicleType").HasConversion<string>().HasMaxLength(20).IsRequired();
        });
    }
}
