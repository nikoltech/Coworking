namespace Coworking.API.Models.Responses;

public record CreateBookingResponse
(
    Guid AccessCode,
    int BookingId
);