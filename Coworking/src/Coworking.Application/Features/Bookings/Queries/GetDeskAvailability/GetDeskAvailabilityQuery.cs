using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responces;
using MediatR;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

public record GetDeskAvailabilityQuery(
    int DeskId,
    DateOnly TargetDate) : IRequest<DeskAvailabilityResponse>;
