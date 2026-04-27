namespace Coworking.API.Models.Responces;

public record TimeSlotResponse
(
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAvailable
);
