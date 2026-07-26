namespace Coworking.Domain.Services.Availability;

public readonly record struct AvailabilityInterval(DateTimeOffset Start, DateTimeOffset End, bool IsAvailable);
