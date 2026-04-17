using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responces;
using MediatR;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

public record GetDeskAvailabilityQuery(
    int DeskId,
    DateTimeOffset TargetDate) : IRequest<DeskAvailabilityResponse>;
