using FluentValidation;

namespace Coworking.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Start < End and not in past
/// </summary>
public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.StartTime)
            .NotEmpty()
            .Must(start => start > DateTimeOffset.UtcNow)
            .WithMessage("Booking cannot be in the past.");

        RuleFor(x => x.EndTime)
            .NotEmpty()
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after the start time.");

        RuleFor(x => x.DeskId)
            .NotEmpty();
    }
}
