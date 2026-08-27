using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Commands.Cancel;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications;
using Coworking.Domain.Common.StateMachine;
using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.UnitTests.TestSupport;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Coworking.UnitTests.Bookings;

/// <summary>
/// Branches of the cancel handler and what it does with its collaborators.
/// Status codes belong to the integration tests.
/// </summary>
public class CancelBookingCommandHandlerTests : IDisposable
{
    private readonly SqliteContext _sqlite = new();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public void Dispose() => _sqlite.Dispose();

    [Fact]
    public async Task Handle_SetsStatusToCancelled()
    {
        var booking = BookingFactory.Seeded(_sqlite.Db, BookingStatus.PendingPayment);

        await Handle(booking.AccessCode);

        Assert.Equal(BookingStatus.Cancelled, await StatusInDatabase(booking.Id));
    }

    [Fact]
    public async Task Handle_PublishesNotification_WithBookingDetails()
    {
        var booking = BookingFactory.Seeded(_sqlite.Db, BookingStatus.PendingPayment);

        await Handle(booking.AccessCode);

        var published = Captured();

        Assert.Equal(BookingFactory.UserEmail, published.UserEmail);
        Assert.Equal(BookingFactory.UserName, published.UserName);
        Assert.Equal(BookingFactory.DeskName, published.DeskName);
        Assert.Equal(BookingFactory.CoworkingName, published.CoworkingName);
        Assert.Equal(BookingFactory.Start, published.Start);
        Assert.Equal(BookingFactory.End, published.End);
        Assert.Equal(BookingFactory.TimeZoneId, published.TimeZoneId);
        Assert.Equal(CancellationReasons.ByUser, published.CancellationReason);
    }

    [Fact]
    public async Task Handle_WhenAccessCodeUnknown_ThrowsNotFound()
    {
        BookingFactory.Seeded(_sqlite.Db, BookingStatus.PendingPayment);

        await Assert.ThrowsAsync<NotFoundException>(() => Handle(Guid.CreateVersion7()));

        await _mediator.DidNotReceive().Publish(Arg.Any<BookingCancelledNotification>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired)]
    public async Task Handle_WhenTerminal_ThrowsInvalidTransition(BookingStatus terminal)
    {
        var booking = BookingFactory.Seeded(_sqlite.Db, terminal);

        await Assert.ThrowsAsync<InvalidTransitionException<BookingStatus>>(() => Handle(booking.AccessCode));

        await _mediator.DidNotReceive().Publish(Arg.Any<BookingCancelledNotification>(), Arg.Any<CancellationToken>());
        Assert.Equal(terminal, await StatusInDatabase(booking.Id));
    }

    [Fact]
    public async Task Handle_WhenPublishThrows_RollsBack()
    {
        var booking = BookingFactory.Seeded(_sqlite.Db, BookingStatus.PendingPayment);

        _mediator.Publish(Arg.Any<BookingCancelledNotification>(), Arg.Any<CancellationToken>())
                 .ThrowsAsync(new InvalidOperationException("broker down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(booking.AccessCode));

        Assert.Equal(BookingStatus.PendingPayment, await StatusInDatabase(booking.Id));
    }

    [Fact]
    public async Task Handle_WhenRowVanishedMidFlight_ThrowsConflict()
    {
        var booking = BookingFactory.Seeded(_sqlite.Db, BookingStatus.PendingPayment);

        // no xmin on SQLite, but deleting the row raises the same DbUpdateConcurrencyException
        _mediator.When(m => m.Publish(Arg.Any<BookingCancelledNotification>(), Arg.Any<CancellationToken>()))
                 .Do(_ => DeleteFromAnotherContext(booking.Id));

        await Assert.ThrowsAsync<ConflictException>(() => Handle(booking.AccessCode));
    }

    // helpers

    private Task Handle(Guid accessCode) =>
        new CancelBookingCommandHandler(_mediator, _sqlite.Db)
            .Handle(new CancelBookingCommand(accessCode), CancellationToken.None);

    private BookingCancelledNotification Captured() =>
        (BookingCancelledNotification)_mediator.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IMediator.Publish))
            .GetArguments()[0]!;

    private async Task<BookingStatus> StatusInDatabase(int bookingId)
    {
        await using var context = _sqlite.NewContext();

        return await context.Set<Booking>()
            .Where(b => b.Id == bookingId)
            .Select(b => b.Status)
            .SingleAsync();
    }

    private void DeleteFromAnotherContext(int bookingId)
    {
        using var context = _sqlite.NewContext();

        context.Set<Booking>().Remove(context.Set<Booking>().Find(bookingId)!);
        context.SaveChanges();
    }
}
