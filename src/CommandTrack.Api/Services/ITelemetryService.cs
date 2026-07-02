using CommandTrack.Domain.Entities;

namespace CommandTrack.Api.Services;

public interface ITelemetryService
{
    Task<UnitTelemetry?> CreateAsync(
        Guid unitId,
        double batteryPercent,
        double latitude,
        double longitude,
        double speedKph,
        DateTimeOffset? recordedAtUtc,
        CancellationToken cancellationToken = default);

    Task<UnitTelemetry?> GetLatestAsync(
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UnitTelemetry>?> GetHistoryAsync(
        Guid unitId,
        int limit,
        CancellationToken cancellationToken = default);
}