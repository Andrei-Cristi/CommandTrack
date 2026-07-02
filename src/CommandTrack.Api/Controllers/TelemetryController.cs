using CommandTrack.Api.Services;
using CommandTrack.Domain.Entities;
using CommandTrack.Shared.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace CommandTrack.Api.Controllers;

[ApiController]
[Route("api/units/{unitId:guid}/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetryService;

    public TelemetryController(
        ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(UnitTelemetryDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitTelemetryDto>> Create(
        Guid unitId,
        CreateUnitTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            UnitTelemetry? telemetry =
                await _telemetryService.CreateAsync(
                    unitId,
                    request.BatteryPercent,
                    request.Latitude,
                    request.Longitude,
                    request.SpeedKph,
                    request.RecordedAtUtc,
                    cancellationToken);

            if (telemetry is null)
            {
                ProblemDetails problem = new()
                {
                    Title = "Unit not found.",
                    Detail =
                        $"No unit with id '{unitId}' was found.",
                    Status = StatusCodes.Status404NotFound
                };

                return NotFound(problem);
            }

            UnitTelemetryDto response =
                MapToDto(telemetry);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        catch (ArgumentException exception)
        {
            ProblemDetails problem = new()
            {
                Title = "Invalid telemetry data.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problem);
        }
    }

    [HttpGet("latest")]
    [ProducesResponseType(
        typeof(UnitTelemetryDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitTelemetryDto>> GetLatest(
        Guid unitId,
        CancellationToken cancellationToken)
    {
        UnitTelemetry? telemetry =
            await _telemetryService.GetLatestAsync(
                unitId,
                cancellationToken);

        if (telemetry is null)
        {
            ProblemDetails problem = new()
            {
                Title = "Telemetry not found.",
                Detail =
                    $"No telemetry was found for unit '{unitId}'.",
                Status = StatusCodes.Status404NotFound
            };

            return NotFound(problem);
        }

        return Ok(MapToDto(telemetry));
    }

    [HttpGet("history")]
    [ProducesResponseType(
    typeof(IEnumerable<UnitTelemetryDto>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<UnitTelemetryDto>>> GetHistory(
    Guid unitId,
    [FromQuery] int limit = 50,
    CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<UnitTelemetry>? history =
            await _telemetryService.GetHistoryAsync(
                unitId,
                limit,
                cancellationToken);

        if (history is null)
        {
            ProblemDetails problem = new()
            {
                Title = "Unit not found.",
                Detail = $"No unit with id '{unitId}' was found.",
                Status = StatusCodes.Status404NotFound
            };

            return NotFound(problem);
        }

        IEnumerable<UnitTelemetryDto> response =
            history.Select(MapToDto);

        return Ok(response);
    }
    private static UnitTelemetryDto MapToDto(
        UnitTelemetry telemetry)
    {
        return new UnitTelemetryDto(
            telemetry.Id,
            telemetry.UnitId,
            telemetry.BatteryPercent,
            telemetry.Latitude,
            telemetry.Longitude,
            telemetry.SpeedKph,
            telemetry.RecordedAtUtc);
    }
}