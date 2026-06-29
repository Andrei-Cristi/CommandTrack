using System.Collections.Concurrent;
using CommandTrack.Domain.Entities;

namespace CommandTrack.Api.Services;

public sealed class InMemoryUnitService : IUnitService
{
    private readonly ConcurrentDictionary<Guid, OperationalUnit> _units = new();

    public IReadOnlyCollection<OperationalUnit> GetAll()
    {
        return _units.Values
            .OrderBy(unit => unit.CallSign)
            .ToArray();
    }

    public OperationalUnit? GetById(Guid id)
    {
        _units.TryGetValue(id, out OperationalUnit? unit);

        return unit;
    }

    public OperationalUnit Create(string callSign, string type)
    {
        bool callSignAlreadyExists = _units.Values.Any(unit =>
            string.Equals(
                unit.CallSign,
                callSign,
                StringComparison.OrdinalIgnoreCase));

        if (callSignAlreadyExists)
        {
            throw new InvalidOperationException(
                $"A unit with call sign '{callSign}' already exists.");
        }

        OperationalUnit unit = new(callSign, type);

        bool unitAdded = _units.TryAdd(unit.Id, unit);

        if (!unitAdded)
        {
            throw new InvalidOperationException(
                "The unit could not be registered.");
        }

        return unit;
    }
}
