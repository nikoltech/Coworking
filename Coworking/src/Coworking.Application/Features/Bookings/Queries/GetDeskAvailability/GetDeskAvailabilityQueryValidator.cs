using FluentValidation;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

public class GetDeskAvailabilityQueryValidator : AbstractValidator<GetDeskAvailabilityQuery>
{
    private const int MaxRangeDays = 90;

    public GetDeskAvailabilityQueryValidator()
    {
        RuleFor(x => x.DeskId)
            .GreaterThan(0);

        RuleFor(x => x.DateFrom)
            .Must(d => d != DateOnly.MinValue)
            .WithMessage("dateFrom is required.");

        RuleFor(x => x.DateTo)
            .Must(d => d != DateOnly.MinValue)
            .WithMessage("dateTo is required.")
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .WithMessage("dateTo must be on or after dateFrom.");

        RuleFor(x => x)
            .Must(x => x.DateTo.DayNumber - x.DateFrom.DayNumber < MaxRangeDays)
            .WithMessage($"Date range cannot exceed {MaxRangeDays} days.");
    }
}
