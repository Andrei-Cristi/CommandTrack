using CommandTrack.Domain.Entities;

namespace CommandTrack.Api.Services;

public interface IUnitService
{
    IReadOnlyCollection<OperationalUnit> GetAll();

    OperationalUnit? GetById(Guid id);

    OperationalUnit Create(string callSign, string type);
}