using Coworking.Application.Common.Enums;
using Coworking.Application.Features.Bookings.Commands.Create;
using Coworking.Application.Ports;
using Coworking.Application.Ports.Synchronization;
using Coworking.Application.Ports.Transactions;
using Coworking.Domain.Entities;
using Coworking.Domain.Exceptions;
using Coworking.Domain.Policies.Rounding;
using Coworking.Domain.ValueObjects;
using MediatR;
using NSubstitute;
using CoworkingEntity = Coworking.Domain.Entities.Coworking;

namespace Coworking.UnitTests.Bookings;

/// <summary>
/// Working hours as the create handler applies them: requests arrive in any offset
/// and must be judged in the coworking's own time zone.
/// </summary>
public class CreateBookingWorkingHoursTests
{
    private const string Kyiv = "Europe/Kyiv";
    private const string Kathmandu = "Asia/Kathmandu";

    private static readonly TimeSpan KyivSummer = TimeSpan.FromHours(3);
    private static readonly TimeSpan KathmanduOffset = new(5, 45, 0);

    // a summer date, so Kyiv is at +03:00
    private static readonly DateOnly Day = new(2030, 7, 1);

    private readonly IBookingAccessCoordinator _coordinator = Substitute.For<IBookingAccessCoordinator>();

    // Kyiv

    [Fact]
    public async Task Kyiv_UtcRequestInsideLocalHours_IsAccepted()
    {
        await Handle(Kyiv, Utc(6, 0), Utc(7, 0));

        AssertLockedInterval(Local(9, 0, KyivSummer), Local(10, 0, KyivSummer));
    }

    [Fact]
    public async Task Kyiv_UtcRequestBeforeLocalOpening_IsRejected()
    {
        // 08:30 in Kyiv, although 05:30 UTC
        var ex = await Assert.ThrowsAsync<DomainException>(() => Handle(Kyiv, Utc(5, 30), Utc(7, 0)));

        Assert.Contains("start time is outside", ex.Message);
    }

    [Fact]
    public async Task Kyiv_UtcRequestAfterLocalClosing_IsRejected()
    {
        // 18:30 in Kyiv; 15:30 UTC would pass a check made in UTC
        var ex = await Assert.ThrowsAsync<DomainException>(() => Handle(Kyiv, Utc(14, 30), Utc(15, 30)));

        Assert.Contains("end time is outside", ex.Message);
    }

    [Fact]
    public async Task Kyiv_RequestInForeignOffset_IsConvertedToLocalTime()
    {
        var newYork = TimeSpan.FromHours(-4);

        // 02:00 in New York is 09:00 in Kyiv
        await Handle(Kyiv, At(Day, 2, 0, newYork), At(Day, 3, 0, newYork));

        AssertLockedInterval(Local(9, 0, KyivSummer), Local(10, 0, KyivSummer));
    }

    [Fact]
    public async Task Kyiv_PeriodSpanningClosedNights_IsLockedAsOneInterval()
    {
        var end = At(Day.AddDays(2), 11, 0, KyivSummer);

        await Handle(Kyiv, Local(10, 0, KyivSummer), end);

        AssertLockedInterval(Local(10, 0, KyivSummer), end);
    }

    // Kathmandu: +05:45, so UTC slot borders are not local slot borders

    [Fact]
    public async Task Kathmandu_LocalRequestInsideHours_IsAccepted()
    {
        var ex = await Record.ExceptionAsync(() =>
            Handle(Kathmandu, Local(10, 15, KathmanduOffset), Local(11, 15, KathmanduOffset)));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Kathmandu_RequestAtOpening_IsAccepted()
    {
        await Handle(Kathmandu, Local(9, 0, KathmanduOffset), Local(10, 0, KathmanduOffset));

        AssertLockedInterval(Local(9, 0, KathmanduOffset), Local(10, 0, KathmanduOffset));
    }

    [Fact]
    public async Task Kathmandu_LocalSlotBorders_AreKept()
    {
        await Handle(Kathmandu, Local(10, 0, KathmanduOffset), Local(10, 30, KathmanduOffset));

        AssertLockedInterval(Local(10, 0, KathmanduOffset), Local(10, 30, KathmanduOffset));
    }

    private async Task Handle(string timeZoneId, DateTimeOffset start, DateTimeOffset end)
    {
        var coworking = new CoworkingEntity
        {
            Name = "Test",
            TimeZoneId = timeZoneId,
            SlotSize = SlotSize.ThirtyMinutes,
            OpenTime = new TimeOnly(9, 0),
            CloseTime = new TimeOnly(18, 0)
        };

        var desk = new Desk { Name = "D1", Coworking = coworking };

        var coworkingRepo = Substitute.For<ICoworkingRepository>();
        coworkingRepo.FindDeskWithCoworkingAsync(1, Arg.Any<CancellationToken>()).Returns(desk);

        var dataContext = Substitute.For<IAppDbContext>();
        dataContext
            .BeginTransactionAsync(Arg.Any<TransactionIsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(Substitute.For<ITransaction>());

        _coordinator
            .WaitIfOverlappingAsync(default, default, default, default)
            .ReturnsForAnyArgs(Substitute.For<IAsyncDisposable>());

        var handler = new CreateBookingCommandHandler(
            Substitute.For<IMediator>(),
            dataContext,
            Substitute.For<IBookingRepository>(),
            coworkingRepo,
            new DefaultRoundingPolicy(),
            _coordinator);

        await handler.Handle(
            new CreateBookingCommand(1, "probe@example.com", "Probe", start, end, null),
            CancellationToken.None);
    }

    // the coordinator is the first collaborator to see the validated, rounded interval
    private void AssertLockedInterval(DateTimeOffset start, DateTimeOffset end)
    {
        var args = _coordinator.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IBookingAccessCoordinator.WaitIfOverlappingAsync))
            .GetArguments();

        var lockedStart = (DateTimeOffset)args[1]!;
        var lockedEnd = (DateTimeOffset)args[2]!;

        Assert.Equal((start.DateTime, start.Offset), (lockedStart.DateTime, lockedStart.Offset));
        Assert.Equal((end.DateTime, end.Offset), (lockedEnd.DateTime, lockedEnd.Offset));
    }

    private static DateTimeOffset Utc(int hour, int minute) => At(Day, hour, minute, TimeSpan.Zero);

    private static DateTimeOffset Local(int hour, int minute, TimeSpan offset) => At(Day, hour, minute, offset);

    private static DateTimeOffset At(DateOnly day, int hour, int minute, TimeSpan offset) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), offset);
}
