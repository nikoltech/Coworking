using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responses;
using MediatR;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

public record GetDeskAvailabilityQuery(
    int DeskId,
    DateOnly DateFrom,
    DateOnly DateTo) : IRequest<DeskAvailabilityResponse>;
