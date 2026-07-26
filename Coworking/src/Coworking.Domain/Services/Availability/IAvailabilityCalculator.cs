namespace Coworking.Domain.Services.Availability;

public interface IAvailabilityCalculator
{
    IReadOnlyList<AvailabilityInterval> Calculate(
        DateOnly from, DateOnly to,
        TimeOnly openTime, TimeOnly closeTime,
        TimeZoneInfo timeZone,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy);
}
