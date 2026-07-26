namespace Coworking.API.Models.Responces;

public record AvailabilityIntervalResponse
(
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAvailable
);
