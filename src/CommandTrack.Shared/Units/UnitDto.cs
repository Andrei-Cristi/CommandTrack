namespace CommandTrack.Shared.Units;

public sealed record UnitDto(
    Guid Id,
    string CallSign,
    string Type,
    string Status,
    DateTimeOffset CreatedAtUtc);