using CommandTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommandTrack.Infrastructure.Persistence;

public sealed class CommandTrackDbContext : DbContext
{
    public CommandTrackDbContext(
        DbContextOptions<CommandTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<OperationalUnit> OperationalUnits =>
        Set<OperationalUnit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CommandTrackDbContext).Assembly);
    }
}