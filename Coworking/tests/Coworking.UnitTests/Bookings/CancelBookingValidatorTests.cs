using Coworking.Application.Features.Bookings.Commands.Cancel;

namespace Coworking.UnitTests.Bookings;

public class CancelBookingValidatorTests
{
    /// Guid is not serializable for xUnit, so the rows carry it as text to stay enumerable.
    public static TheoryData<string, string, bool> AccessCodes => new()
    {
        { "empty", Guid.Empty.ToString(), false },
        { "wrong version", Guid.NewGuid().ToString(), false },
        { "version 7", Guid.CreateVersion7().ToString(), true }
    };

    [Theory]
    [MemberData(nameof(AccessCodes))]
    public void AccessCode_IsAcceptedOnlyWhenItIsVersion7(string label, string accessCode, bool expected)
    {
        var result = new CancelBookingValidator()
            .Validate(new CancelBookingCommand(Guid.Parse(accessCode)));

        Assert.True(result.IsValid == expected, $"{label}: expected IsValid={expected}");
    }
}
