using CommandTrack.Api.Controllers;
using CommandTrack.Api.Services;
using CommandTrack.Domain.Entities;
using CommandTrack.Domain.Enums;
using CommandTrack.Shared.Units;
using Microsoft.AspNetCore.Mvc;

namespace CommandTrack.Api.Tests;

public sealed class UnitsControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedUnit()
    {
        FakeUnitService service = new();
        UnitsController controller = new(service);

        ActionResult<UnitDto> result = await controller.Create(
            new CreateUnitRequest("ROBOT-TEST", "Ground Robot"),
            CancellationToken.None);

        CreatedAtActionResult created =
            Assert.IsType<CreatedAtActionResult>(result.Result);
        UnitDto response = Assert.IsType<UnitDto>(created.Value);

        Assert.Equal(nameof(UnitsController.GetById), created.ActionName);
        Assert.Equal("ROBOT-TEST", response.CallSign);
        Assert.Equal("Offline", response.Status);
    }

    [Fact]
    public async Task Create_ReturnsConflictForDuplicateCallSign()
    {
        FakeUnitService service = new()
        {
            CreateException = new InvalidOperationException(
                "A unit with this call sign already exists.")
        };
        UnitsController controller = new(service);

        ActionResult<UnitDto> result = await controller.Create(
            new CreateUnitRequest("ROBOT-TEST", "Ground Robot"),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsBadRequestForUnknownStatus()
    {
        UnitsController controller = new(new FakeUnitService());

        ActionResult<UnitDto> result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new UpdateUnitStatusRequest("Unknown"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RegisterHeartbeat_ReturnsNotFoundForUnknownUnit()
    {
        UnitsController controller = new(new FakeUnitService());

        ActionResult<UnitDto> result =
            await controller.RegisterHeartbeat(
                Guid.NewGuid(),
                CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private sealed class FakeUnitService : IUnitService
    {
        public Exception? CreateException { get; init; }

        public Task<IReadOnlyCollection<OperationalUnit>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<OperationalUnit>>(
                Array.Empty<OperationalUnit>());
        }

        public Task<OperationalUnit?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<OperationalUnit?>(null);
        }

        public Task<OperationalUnit?> UpdateStatusAsync(
            Guid id,
            UnitStatus status,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<OperationalUnit?>(null);
        }

        public Task<OperationalUnit?> RegisterHeartbeatAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<OperationalUnit?>(null);
        }

        public Task<OperationalUnit> CreateAsync(
            string callSign,
            string type,
            CancellationToken cancellationToken = default)
        {
            if (CreateException is not null)
            {
                return Task.FromException<OperationalUnit>(
                    CreateException);
            }

            return Task.FromResult(
                new OperationalUnit(callSign, type));
        }
    }
}
