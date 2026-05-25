using FluentValidation;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

public class GetDeskAvailabilityQueryValidator : AbstractValidator<GetDeskAvailabilityQuery>
{
    public GetDeskAvailabilityQueryValidator()
    {
        RuleFor(x => x.DeskId)
            .GreaterThan(0);

        RuleFor(x => x.TargetDate)
            .Must(d => d != DateOnly.MinValue)
            .WithMessage("targetDate is required.");
    }
}
