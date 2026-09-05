using AutoMapper;
using Coworking.API.Controllers.Abstractions;
using Coworking.API.Models.Requests;
using Coworking.API.Models.Responces;
using Coworking.Application.Features.Bookings.Commands.Cancel;
using Coworking.Application.Features.Bookings.Commands.Create;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coworking.API.Controllers;

[Route("api/bookings")]
[Tags("Bookings")]
public sealed class BookingsController(IMediator mediator, IMapper mapper) : ApiControllerBase
{
    /// <summary>Carries the access code: a credential in the path would reach every access log.</summary>
    public const string AccessCodeHeader = "Access-Code";

    /// <summary>
    /// Creates a booking.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("booking-write")]
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
    [HttpDelete("{bookingId:int}")]
    [EnableRateLimiting("booking-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(
        [FromRoute] int bookingId,
        [FromHeader(Name = AccessCodeHeader)] Guid accessCode,
        CancellationToken ct)
    {
        await mediator.Send(new CancelBookingCommand(bookingId, accessCode), ct);

        return NoContent();
    }
}