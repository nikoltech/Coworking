namespace Coworking.Domain.Services.SlotGenerator;

public readonly record struct TimeSlot(DateTimeOffset Start, DateTimeOffset End);
