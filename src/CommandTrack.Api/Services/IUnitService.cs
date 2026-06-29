using CommandTrack.Domain.Entities;

namespace CommandTrack.Api.Services;

public interface IUnitService
{
    Task<IReadOnlyCollection<OperationalUnit>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalUnit?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OperationalUnit> CreateAsync(
        string callSign,
        string type,
        CancellationToken cancellationToken = default);
}