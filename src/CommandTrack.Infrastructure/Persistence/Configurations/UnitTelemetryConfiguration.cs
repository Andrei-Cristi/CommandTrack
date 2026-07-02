using CommandTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommandTrack.Infrastructure.Persistence.Configurations;

public sealed class UnitTelemetryConfiguration
    : IEntityTypeConfiguration<UnitTelemetry>
{
    public void Configure(
        EntityTypeBuilder<UnitTelemetry> builder)
    {
        builder.ToTable("UnitTelemetry");

        builder.HasKey(telemetry => telemetry.Id);

        builder.Property(telemetry => telemetry.BatteryPercent)
            .IsRequired();

        builder.Property(telemetry => telemetry.Latitude)
            .IsRequired();

        builder.Property(telemetry => telemetry.Longitude)
            .IsRequired();

        builder.Property(telemetry => telemetry.SpeedKph)
            .IsRequired();

        builder.Property(telemetry => telemetry.RecordedAtUtc)
            .IsRequired();

        builder.HasOne(telemetry => telemetry.Unit)
            .WithMany()
            .HasForeignKey(telemetry => telemetry.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(telemetry => new
        {
            telemetry.UnitId,
            telemetry.RecordedAtUtc
        });
    }
}