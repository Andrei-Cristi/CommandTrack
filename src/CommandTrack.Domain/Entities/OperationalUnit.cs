using CommandTrack.Domain.Enums;

namespace CommandTrack.Domain.Entities;

public sealed class OperationalUnit
{
    public Guid Id { get; private set; }

    public string CallSign { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public UnitStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastSeenAtUtc { get; private set; }

    private OperationalUnit()
    {
    }

    public OperationalUnit(string callSign, string type)
    {
        if (string.IsNullOrWhiteSpace(callSign))
        {
            throw new ArgumentException(
                "Call sign cannot be empty.",
                nameof(callSign));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException(
                "Unit type cannot be empty.",
                nameof(type));
        }

        Id = Guid.NewGuid();
        CallSign = callSign.Trim();
        Type = type.Trim();
        Status = UnitStatus.Offline;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastSeenAtUtc = null;
    }

    public void UpdateStatus(UnitStatus status)
    {
        Status = status;
    }

    public void RegisterHeartbeat()
    {
        LastSeenAtUtc = DateTimeOffset.UtcNow;

        if (Status == UnitStatus.Offline)
        {
            Status = UnitStatus.Online;
        }
    }
}