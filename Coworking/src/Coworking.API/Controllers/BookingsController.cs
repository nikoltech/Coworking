using AutoMapper;
using Coworking.API.Controllers.Abstractions;
using Coworking.API.Models.Requests;
using Coworking.API.Models.Responces;
using Coworking.Application.Features.Bookings.Commands.Cancel;
using Coworking.Application.Features.Bookings.Commands.Create;
using MediatR;
using Coworking.API.Infrastructure.RateLimiting;
using Coworking.API.Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;

namespace Coworking.API.Controllers;

[Route("api/bookings")]
[Tags("Bookings")]
public sealed class BookingsController(IMediator mediator, IMapper mapper) : ApiControllerBase
{
    public const string AccessCodeHeader = "Access-Code";

    /// <summary>
    /// Creates a booking.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.BookingWrite)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CreateBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CreateBookingResponse>> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var command = mapper.Map<CreateBookingCommand>(request);

        var result = await mediator.Send(command, ct);

        return Ok(new CreateBookingResponse(
            result.AccessCode,
            result.BookingId));
    }

    /// <summary>
    /// Cancels a booking. The access code returned on creation is the authorization.
    /// </summary>
    [HttpDelete("{bookingId:long}")]
    [EnableRateLimiting(RateLimitPolicies.BookingWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(
        [FromRoute, PositiveId(long.MaxValue)] long bookingId,
        [FromHeader(Name = AccessCodeHeader), Required] Guid accessCode,
        CancellationToken ct)
    {
        await mediator.Send(new CancelBookingCommand(bookingId, accessCode), ct);

        return NoContent();
    }
}