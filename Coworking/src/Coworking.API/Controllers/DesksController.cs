using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Coworking.API.Controllers.Abstractions;
using Coworking.API.Models.Requests;
using Coworking.API.Models.Responces;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;
using Coworking.Application.Features.Desks.Queries.GetDesks;
using Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coworking.API.Controllers;

[Route("api/desks")]
[Tags("Desks")]
public sealed class DesksController(IMediator mediator, IMapper mapper) : ApiControllerBase
{
    /// <summary>
    /// Returns desks by coworking id.
    /// </summary>
    [HttpGet("{coworkingId:int}")]
    [EnableRateLimiting("read-heavy")]
    [ProducesResponseType(typeof(IReadOnlyList<DeskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeskDto>>> Get([FromRoute] int coworkingId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDesksQuery(coworkingId), ct);

        return Ok(result);
    }

    /// <summary>
    /// Returns desk availability for a date range. Slots are sorted by start time.
    /// </summary>
    [HttpGet("{deskId:int}/availability")]
    [EnableRateLimiting("read-heavy")]
    [ProducesResponseType(typeof(DeskAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeskAvailabilityResponse>> GetAvailability(
        [FromRoute] int deskId,
        [FromQuery, Required] DateOnly? dateFrom,
        [FromQuery, Required] DateOnly? dateTo,
        CancellationToken ct)
    {
        var query = mapper.Map<GetDeskAvailabilityQuery>(new GetDeskAvailabilityRequest(deskId, dateFrom, dateTo));

        var result = await mediator.Send(query, ct);

        return Ok(mapper.Map<DeskAvailabilityResponse>(result));
    }
}