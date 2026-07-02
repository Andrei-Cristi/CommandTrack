namespace CommandTrack.Shared.Telemetry;

public sealed record UnitTelemetryDto(
    Guid Id,
    Guid UnitId,
    double BatteryPercent,
    double Latitude,
    double Longitude,
    double SpeedKph,
    DateTimeOffset RecordedAtUtc);