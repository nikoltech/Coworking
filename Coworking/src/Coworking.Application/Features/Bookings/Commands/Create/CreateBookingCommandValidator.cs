using Coworking.Application.Features.Bookings.Commands.Create.Requests;
using Coworking.Domain.Constants;
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

        RuleFor(x => x.UserEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(BookingLimits.UserEmailMaxLength);

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(BookingLimits.UserNameMaxLength);

        RuleFor(x => x.StartTime)
            .Must(IsWholeMinute)
            .WithMessage("StartTime must be rounded to minutes.");

        RuleFor(x => x.EndTime)
            .Must(IsWholeMinute)
            .WithMessage("EndTime must be rounded to minutes.");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Cannot book in the past.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after the start time.");

        RuleFor(x => x.Metadata)
            .Must(HaveAnyValue!)
            .WithMessage("Metadata object cannot be empty if provided.")
            .When(x => x.Metadata is not null);
    }

    // Ticks, not Second/Millisecond: those are components and miss sub-millisecond ticks.
    private static bool IsWholeMinute(DateTimeOffset time) =>
        time.Ticks % TimeSpan.TicksPerMinute == 0;

    private static bool HaveAnyValue(BookingMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(metadata.UserTimeZoneId);
    }
}
