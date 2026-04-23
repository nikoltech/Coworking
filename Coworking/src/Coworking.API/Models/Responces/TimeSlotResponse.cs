namespace Coworking.API.Models.Responses;

public record TimeSlotResponse
(
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAvailable
);
