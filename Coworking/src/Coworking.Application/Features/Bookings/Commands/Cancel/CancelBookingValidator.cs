using FluentValidation;

namespace Coworking.Application.Features.Bookings.Commands.Cancel;

public class CancelBookingValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingValidator()
    {
        RuleFor(x => x.AccessCode)
            .NotEmpty().WithMessage("AccessCode is required.");
    }
}
