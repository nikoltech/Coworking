namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;

public record AvailabilityIntervalDto(DateTimeOffset Start, DateTimeOffset End, bool IsAvailable);
