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
    [ProducesResponseType(typeof(IEnumerable<UnitDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UnitDto>> GetAll()
    {
        IEnumerable<UnitDto> units = _unitService
            .GetAll()
            .Select(MapToDto);

        return Ok(units);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UnitDto> GetById(Guid id)
    {
        OperationalUnit? unit = _unitService.GetById(id);

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
    public ActionResult<UnitDto> Create(CreateUnitRequest request)
    {
        try
        {
            OperationalUnit unit = _unitService.Create(
                request.CallSign,
                request.Type);

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
