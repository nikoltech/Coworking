using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Services.Availability;

public interface IAvailabilityCalculator
{
    IReadOnlyList<AvailabilityInterval> Calculate(
        DateOnly from, DateOnly to,
        WorkingSchedule schedule,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> busy);
}
