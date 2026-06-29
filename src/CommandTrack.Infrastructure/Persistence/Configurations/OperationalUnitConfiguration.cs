using CommandTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommandTrack.Infrastructure.Persistence.Configurations;

public sealed class OperationalUnitConfiguration
    : IEntityTypeConfiguration<OperationalUnit>
{
    public void Configure(
        EntityTypeBuilder<OperationalUnit> builder)
    {
        builder.ToTable("OperationalUnits");

        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.CallSign)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(unit => unit.CallSign)
            .IsUnique();

        builder.Property(unit => unit.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(unit => unit.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(unit => unit.CreatedAtUtc)
            .IsRequired();
    }
}