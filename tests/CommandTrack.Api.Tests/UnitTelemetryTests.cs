using CommandTrack.Domain.Entities;

namespace CommandTrack.Api.Tests;

public sealed class UnitTelemetryTests
{
    public static TheoryData<double, double, double, double> InvalidValues =>
        new()
        {
            { -1, 44.4268, 26.1025, 10 },
            { 101, 44.4268, 26.1025, 10 },
            { 50, -91, 26.1025, 10 },
            { 50, 91, 26.1025, 10 },
            { 50, 44.4268, -181, 10 },
            { 50, 44.4268, 181, 10 },
            { 50, 44.4268, 26.1025, -1 }
        };

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void Constructor_RejectsInvalidMeasurements(
        double batteryPercent,
        double latitude,
        double longitude,
        double speedKph)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnitTelemetry(
                Guid.NewGuid(),
                batteryPercent,
                latitude,
                longitude,
                speedKph,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_RejectsEmptyUnitId()
    {
        Assert.Throws<ArgumentException>(() =>
            new UnitTelemetry(
                Guid.Empty,
                50,
                44.4268,
                26.1025,
                10,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_StoresTimestampAsUtc()
    {
        DateTimeOffset localTimestamp = new(
            2026,
            8,
            5,
            12,
            0,
            0,
            TimeSpan.FromHours(3));

        UnitTelemetry telemetry = new(
            Guid.NewGuid(),
            50,
            44.4268,
            26.1025,
            10,
            localTimestamp);

        Assert.Equal(TimeSpan.Zero, telemetry.RecordedAtUtc.Offset);
        Assert.Equal(localTimestamp.UtcDateTime, telemetry.RecordedAtUtc);
    }
}
