using CommandTrack.Domain.Entities;
using CommandTrack.Domain.Enums;

namespace CommandTrack.Api.Services;

public interface IUnitService
{
    Task<IReadOnlyCollection<OperationalUnit>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalUnit?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<OperationalUnit?> UpdateStatusAsync(
        Guid id,
    UnitStatus status,
    CancellationToken cancellationToken = default);

    Task<OperationalUnit?> RegisterHeartbeatAsync(
    Guid id,
    CancellationToken cancellationToken = default);

    Task<OperationalUnit> CreateAsync(
        string callSign,
        string type,
        CancellationToken cancellationToken = default);
}