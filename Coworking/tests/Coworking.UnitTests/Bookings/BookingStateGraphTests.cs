using Coworking.Domain.Entities;
using Coworking.Domain.Enums;

namespace Coworking.UnitTests.Bookings;

/// <summary>
/// Boundary rules of the booking graph. The full transition matrix is deliberately not
/// snapshotted: keeping the expected set in step with the graph proves agreement, not
/// correctness.
/// </summary>
public class BookingStateGraphTests
{
    private static readonly BookingStatus[] Terminal = [BookingStatus.Cancelled, BookingStatus.Expired];

    private static readonly BookingStatus[] Active =
    [
        BookingStatus.Created,
        BookingStatus.PendingPayment,
        BookingStatus.PendingConfirmation,
        BookingStatus.Confirmed
    ];

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired)]
    public void TerminalStates_HaveNoOutgoingTransitions(BookingStatus terminal)
    {
        Assert.Empty(Booking.StateGraph.From(terminal));
    }

    [Fact]
    public void Cancelled_IsReachableOnlyFromCancelling()
    {
        var sources = Enum.GetValues<BookingStatus>()
            .Where(from => from != BookingStatus.Cancelled)
            .Where(from => Booking.StateGraph.CanMove(from, BookingStatus.Cancelled));

        Assert.Equal([BookingStatus.Cancelling], sources);
    }

    [Theory]
    [InlineData(BookingStatus.Created)]
    [InlineData(BookingStatus.PendingPayment)]
    [InlineData(BookingStatus.PendingConfirmation)]
    [InlineData(BookingStatus.Confirmed)]
    public void Cancelling_IsReachableFromEveryActiveState(BookingStatus active)
    {
        Assert.True(Booking.StateGraph.CanMove(active, BookingStatus.Cancelling));
    }

    [Fact]
    public void TerminalStates_CannotStartCancelling()
    {
        Assert.All(Terminal, terminal =>
            Assert.False(Booking.StateGraph.CanMove(terminal, BookingStatus.Cancelling)));
    }

    [Fact]
    public void ActiveStates_CannotReachCancelledDirectly()
    {
        Assert.All(Active, active =>
            Assert.False(Booking.StateGraph.CanMove(active, BookingStatus.Cancelled)));
    }
}
