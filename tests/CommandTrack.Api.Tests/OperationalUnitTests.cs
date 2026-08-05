using CommandTrack.Domain.Entities;
using CommandTrack.Domain.Enums;

namespace CommandTrack.Api.Tests;

public sealed class OperationalUnitTests
{
    [Fact]
    public void RegisterHeartbeat_MovesOfflineUnitOnline()
    {
        OperationalUnit unit = new("ROBOT-TEST", "Ground Robot");

        unit.RegisterHeartbeat();

        Assert.Equal(UnitStatus.Online, unit.Status);
        Assert.NotNull(unit.LastSeenAtUtc);
    }

    [Theory]
    [InlineData(UnitStatus.Busy)]
    [InlineData(UnitStatus.Maintenance)]
    public void RegisterHeartbeat_PreservesOperatorControlledStatus(
        UnitStatus status)
    {
        OperationalUnit unit = new("ROBOT-TEST", "Ground Robot");
        unit.UpdateStatus(status);

        unit.RegisterHeartbeat();

        Assert.Equal(status, unit.Status);
        Assert.NotNull(unit.LastSeenAtUtc);
    }
}
