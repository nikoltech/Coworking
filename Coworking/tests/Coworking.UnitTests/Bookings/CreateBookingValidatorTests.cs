using Coworking.Application.Features.Bookings.Commands.Create;
using Coworking.Domain.Constants;

namespace Coworking.UnitTests.Bookings;

public class CreateBookingValidatorTests
{
    private static readonly DateTimeOffset Start =
        new(DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(10), TimeSpan.Zero);

    private readonly CreateBookingValidator _validator = new();

    [Fact]
    public void Duration_AtTheLimit_IsValid()
    {
        var result = _validator.Validate(Command(Start, Start + BookingLimits.MaxDuration));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Duration_OverTheLimit_IsRejected()
    {
        var result = _validator.Validate(Command(Start, Start + BookingLimits.MaxDuration + TimeSpan.FromMinutes(1)));

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateBookingCommand.EndTime), error.PropertyName);
    }

    private static CreateBookingCommand Command(DateTimeOffset start, DateTimeOffset end) =>
        new(1, "probe@example.com", "Probe", start, end, null);
}
