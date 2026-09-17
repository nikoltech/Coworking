using AutoMapper;
using Coworking.API.Controllers.Abstractions;
using Coworking.API.Models.Responces;
using Coworking.Application.Features.Coworkings.Queries.GetCoworkings;
using MediatR;
using Coworking.API.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coworking.API.Controllers;

[Route("api/coworkings")]
[Tags("Coworkings")]
public sealed class CoworkingsController(IMediator mediator, IMapper mapper) : ApiControllerBase
{
    /// <summary>
    /// Returns all coworking spaces.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.ReadHeavy)]
    [ProducesResponseType(typeof(IReadOnlyList<CoworkingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoworkingResponse>>> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCoworkingsQuery(), ct);

        return Ok(mapper.Map<IReadOnlyList<CoworkingResponse>>(result));
    }
}
