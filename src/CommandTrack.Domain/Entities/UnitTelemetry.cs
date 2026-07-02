namespace CommandTrack.Domain.Entities;

public sealed class UnitTelemetry
{
    public Guid Id { get; private set; }

    public Guid UnitId { get; private set; }

    public double BatteryPercent { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public double SpeedKph { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }

    public OperationalUnit Unit { get; private set; } = null!;

    private UnitTelemetry()
    {
    }

    public UnitTelemetry(
        Guid unitId,
        double batteryPercent,
        double latitude,
        double longitude,
        double speedKph,
        DateTimeOffset recordedAtUtc)
    {
        if (unitId == Guid.Empty)
        {
            throw new ArgumentException(
                "Unit ID cannot be empty.",
                nameof(unitId));
        }

        if (batteryPercent < 0 || batteryPercent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batteryPercent),
                "Battery percentage must be between 0 and 100.");
        }

        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                "Latitude must be between -90 and 90.");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                "Longitude must be between -180 and 180.");
        }

        if (speedKph < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speedKph),
                "Speed cannot be negative.");
        }

        Id = Guid.NewGuid();
        UnitId = unitId;
        BatteryPercent = batteryPercent;
        Latitude = latitude;
        Longitude = longitude;
        SpeedKph = speedKph;
        RecordedAtUtc = recordedAtUtc.ToUniversalTime();
    }
}