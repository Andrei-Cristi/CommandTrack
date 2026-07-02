using CommandTrack.Domain.Enums;
using CommandTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommandTrack.Api.Services;

public sealed class UnitOfflineMonitorService : BackgroundService
{
    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromSeconds(5);

    private static readonly TimeSpan OfflineThreshold =
        TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<UnitOfflineMonitorService> _logger;

    public UnitOfflineMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<UnitOfflineMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Unit offline monitoring started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MarkInactiveUnitsOfflineAsync(stoppingToken);

                await Task.Delay(
                    CheckInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while checking inactive units.");

                await Task.Delay(
                    CheckInterval,
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Unit offline monitoring stopped.");
    }

    private async Task MarkInactiveUnitsOfflineAsync(
        CancellationToken cancellationToken)
    {
        using IServiceScope scope =
            _scopeFactory.CreateScope();

        CommandTrackDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<CommandTrackDbContext>();

        DateTimeOffset offlineCutoff =
            DateTimeOffset.UtcNow - OfflineThreshold;

        var inactiveUnits =
            await dbContext.OperationalUnits
                .Where(unit =>
                    unit.LastSeenAtUtc.HasValue &&
                    unit.LastSeenAtUtc.Value < offlineCutoff &&
                    unit.Status != UnitStatus.Offline)
                .ToListAsync(cancellationToken);

        if (inactiveUnits.Count == 0)
        {
            return;
        }

        foreach (var unit in inactiveUnits)
        {
            unit.UpdateStatus(UnitStatus.Offline);

            _logger.LogInformation(
                "Unit {CallSign} was marked Offline. Last seen: {LastSeenAtUtc}",
                unit.CallSign,
                unit.LastSeenAtUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}