namespace CommandTrack.Shared.Telemetry;

public sealed record CreateUnitTelemetryRequest(
    double BatteryPercent,
    double Latitude,
    double Longitude,
    double SpeedKph,
    DateTimeOffset? RecordedAtUtc);