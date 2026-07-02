using CommandTrack.Domain.Entities;
using CommandTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommandTrack.Api.Services;

public sealed class EfTelemetryService : ITelemetryService
{
    private readonly CommandTrackDbContext _dbContext;

    public EfTelemetryService(CommandTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UnitTelemetry?> CreateAsync(
        Guid unitId,
        double batteryPercent,
        double latitude,
        double longitude,
        double speedKph,
        DateTimeOffset? recordedAtUtc,
        CancellationToken cancellationToken = default)
    {
        bool unitExists =
            await _dbContext.OperationalUnits.AnyAsync(
                unit => unit.Id == unitId,
                cancellationToken);

        if (!unitExists)
        {
            return null;
        }

        UnitTelemetry telemetry = new(
            unitId,
            batteryPercent,
            latitude,
            longitude,
            speedKph,
            recordedAtUtc ?? DateTimeOffset.UtcNow);

        _dbContext.UnitTelemetry.Add(telemetry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return telemetry;
    }
    public async Task<UnitTelemetry?> GetLatestAsync(
    Guid unitId,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.UnitTelemetry
            .AsNoTracking()
            .Where(telemetry => telemetry.UnitId == unitId)
            .OrderByDescending(telemetry => telemetry.RecordedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}