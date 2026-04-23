using Coworking.API.Controllers.Abstractions;
using Coworking.Application.Features.Coworkings.Queries.GetCoworkings;
using Coworking.Application.Features.Coworkings.Queries.GetCoworkings.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coworking.API.Controllers;

[Route("api/coworkings")]
[Tags("Coworkings")]
public sealed class CoworkingsController(
    IMediator mediator) : ApiControllerBase
{
    /// <summary>
    /// Returns all coworking spaces.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("read-heavy")]
    [ProducesResponseType(typeof(IReadOnlyList<CoworkingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoworkingDto>>> Get(
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetCoworkingsQuery(), ct);

        return Ok(result);
    }
}