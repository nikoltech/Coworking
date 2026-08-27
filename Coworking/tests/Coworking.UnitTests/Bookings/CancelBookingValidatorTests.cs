using Coworking.Application.Features.Bookings.Commands.Cancel;

namespace Coworking.UnitTests.Bookings;

public class CancelBookingValidatorTests
{
    [Fact]
    public void EmptyAccessCode_IsRejected()
    {
        var result = new CancelBookingValidator().Validate(new CancelBookingCommand(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RealAccessCode_IsAccepted()
    {
        var result = new CancelBookingValidator().Validate(new CancelBookingCommand(Guid.CreateVersion7()));

        Assert.True(result.IsValid);
    }
}
