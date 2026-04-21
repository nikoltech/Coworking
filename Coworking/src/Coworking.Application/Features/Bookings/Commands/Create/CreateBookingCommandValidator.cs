using FluentValidation;

namespace Coworking.Application.Features.Bookings.Commands.Create;

/// <summary>
/// Start < End && not in past
/// 
/// </summary>
public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.DeskId).GreaterThan(0);

        RuleFor(x => x.StartTime)
            .Must(t => t.Second == 0 && t.Millisecond == 0)
            .WithMessage("StartTime must be rounded to minutes.");

        RuleFor(x => x.EndTime)
            .Must(t => t.Second == 0 && t.Millisecond == 0)
            .WithMessage("EndTime must be rounded to minutes.");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Cannot book in the past.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after the start time.");

    }
}
