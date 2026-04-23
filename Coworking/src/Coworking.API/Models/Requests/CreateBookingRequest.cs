namespace Coworking.API.Models.Requests;

public record CreateBookingRequest
(
    int DeskId,
    string UserEmail,
    string UserName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingMetadataRequest? Metadata
);