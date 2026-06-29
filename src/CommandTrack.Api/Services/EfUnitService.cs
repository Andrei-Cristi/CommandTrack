using CommandTrack.Domain.Entities;
using CommandTrack.Domain.Enums;
using CommandTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommandTrack.Api.Services;

public sealed class EfUnitService : IUnitService
{
    private readonly CommandTrackDbContext _dbContext;

    public EfUnitService(CommandTrackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<OperationalUnit>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationalUnits
            .AsNoTracking()
            .OrderBy(unit => unit.CallSign)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationalUnit?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationalUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                unit => unit.Id == id,
                cancellationToken);
    }

    public async Task<OperationalUnit> CreateAsync(
        string callSign,
        string type,
        CancellationToken cancellationToken = default)
    {
        string normalizedCallSign = callSign.Trim();

        bool callSignAlreadyExists =
            await _dbContext.OperationalUnits.AnyAsync(
                unit => unit.CallSign == normalizedCallSign,
                cancellationToken);

        if (callSignAlreadyExists)
        {
            throw new InvalidOperationException(
                $"A unit with call sign '{normalizedCallSign}' already exists.");
        }

        OperationalUnit unit = new(normalizedCallSign, type);

        _dbContext.OperationalUnits.Add(unit);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return unit;
    }

    public async Task<OperationalUnit?> UpdateStatusAsync(
        Guid id,
        UnitStatus status,
        CancellationToken cancellationToken = default)
    {
        OperationalUnit? unit =
            await _dbContext.OperationalUnits.FirstOrDefaultAsync(
                unit => unit.Id == id,
                cancellationToken);

        if (unit is null)
        {
            return null;
        }

        unit.UpdateStatus(status);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return unit;
    }
}