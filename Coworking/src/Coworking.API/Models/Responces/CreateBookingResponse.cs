namespace Coworking.API.Models.Responces;

public record CreateBookingResponse
(
    Guid AccessCode,
    long BookingId
);