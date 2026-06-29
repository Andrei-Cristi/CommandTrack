using CommandTrack.Api.Services;
using CommandTrack.Domain.Entities;
using CommandTrack.Shared.Units;
using Microsoft.AspNetCore.Mvc;

namespace CommandTrack.Api.Controllers;

[ApiController]
[Route("api/units")]
public sealed class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IEnumerable<UnitDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UnitDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<OperationalUnit> units =
            await _unitService.GetAllAsync(cancellationToken);

        return Ok(units.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        OperationalUnit? unit =
            await _unitService.GetByIdAsync(id, cancellationToken);

        if (unit is null)
        {
            return NotFound();
        }

        return Ok(MapToDto(unit));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UnitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UnitDto>> Create(
        CreateUnitRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            OperationalUnit unit =
                await _unitService.CreateAsync(
                    request.CallSign,
                    request.Type,
                    cancellationToken);

            UnitDto response = MapToDto(unit);

            return CreatedAtAction(
                nameof(GetById),
                new { id = unit.Id },
                response);
        }
        catch (ArgumentException exception)
        {
            ProblemDetails problem = new()
            {
                Title = "Invalid unit data.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problem);
        }
        catch (InvalidOperationException exception)
        {
            ProblemDetails problem = new()
            {
                Title = "The unit could not be created.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            };

            return Conflict(problem);
        }
    }

    private static UnitDto MapToDto(OperationalUnit unit)
    {
        return new UnitDto(
            unit.Id,
            unit.CallSign,
            unit.Type,
            unit.Status.ToString(),
            unit.CreatedAtUtc);
    }
}