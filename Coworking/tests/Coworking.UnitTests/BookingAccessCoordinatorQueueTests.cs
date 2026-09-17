using Coworking.Application.Common.Exceptions;
using Coworking.Infrastructure.Synchronization.InMemory;
using Microsoft.Extensions.Time.Testing;

namespace Coworking.UnitTests;

/// <summary>
/// Queueing, waiting budget and lease expiry. Time is fake, so timeouts and expiry need no real
/// waiting; a short real pause is only used to let woken waiters settle before asserting.
/// </summary>
public class BookingAccessCoordinatorQueueTests
{
    private static readonly DateTimeOffset Ten = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan LeaseLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LongBudget = TimeSpan.FromMinutes(10);

    // a broken coordinator must fail the test, not hang the run
    private const int TestTimeout = 10_000;

    // fails a single wait early, with a clearer message than the test timeout
    private static readonly TimeSpan Guard = TimeSpan.FromSeconds(5);

    private readonly FakeTimeProvider _time = new();
    private readonly InMemoryBookingAccessCoordinator _coordinator;

    public BookingAccessCoordinatorQueueTests() => _coordinator = new(_time);

    [Theory(Timeout = TestTimeout)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Queue_ServesOverlappingRequestsOneByOne(int waiterCount)
    {
        var current = await Acquire(Ten, Ten.AddHours(1));

        var pending = Enumerable.Range(0, waiterCount)
            .Select(_ => Acquire(Ten, Ten.AddHours(1)))
            .ToList();

        await AssertStillWaiting(pending);

        for (var served = 0; served < waiterCount; served++)
        {
            await current.DisposeAsync();

            var next = await Task.WhenAny(pending).WaitAsync(Guard);
            pending.Remove(next);

            // one released lease lets exactly one waiter in
            await AssertStillWaiting(pending);

            current = await next;
        }

        await current.DisposeAsync();
    }

    // A holds 10:00–11:00, B waits for 10:30–11:30, C asks for 11:15–12:00:
    // C overlaps only the waiting B, so it is granted; B then waits for C as well
    [Fact(Timeout = TestTimeout)]
    public async Task Request_IsCheckedAgainstHoldersOnly_NotAgainstWaiters()
    {
        var a = await Acquire(Ten, Ten.AddHours(1));
        var b = Acquire(Ten.AddMinutes(30), Ten.AddMinutes(90));

        var c = await Acquire(Ten.AddMinutes(75), Ten.AddHours(2)).WaitAsync(Guard);

        await a.DisposeAsync();
        await AssertStillWaiting([b]);

        await c.DisposeAsync();
        await using var bLease = await b.WaitAsync(Guard);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task BudgetRunsOut_ThrowsServiceBusy_AndDoesNotHoldBackOthers()
    {
        var holder = await Acquire(Ten, Ten.AddHours(1));

        var impatient = Enumerable.Range(0, 5)
            .Select(_ => _coordinator.WaitIfOverlappingAsync(1, Ten, Ten.AddHours(1), default))
            .ToList();

        var patient = Acquire(Ten, Ten.AddHours(1));

        _time.Advance(InMemoryBookingAccessCoordinator.DefaultAcquireTimeout);

        foreach (var waiter in impatient)
            await Assert.ThrowsAsync<ServiceBusyException>(() => waiter.WaitAsync(Guard));

        await AssertStillWaiting([patient]);

        await holder.DisposeAsync();
        await using var lease = await patient.WaitAsync(Guard);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Budget_CoversTheWholeRequest_NotEachWait()
    {
        await using var first = await Acquire(Ten, Ten.AddHours(1));

        var waiter = _coordinator.WaitIfOverlappingAsync(
            InMemoryBookingAccessCoordinator.DefaultAcquireTimeout, 1, Ten.AddMinutes(30), Ten.AddMinutes(90), default);

        await using var second = await Acquire(Ten.AddHours(1), Ten.AddHours(2)).WaitAsync(Guard);

        _time.Advance(TimeSpan.FromSeconds(20));
        await first.DisposeAsync();

        // the waiter woke up, found the second lease and waits again with 10 s left
        _time.Advance(TimeSpan.FromSeconds(9));
        await AssertStillWaiting([waiter]);

        _time.Advance(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<ServiceBusyException>(() => waiter.WaitAsync(Guard));
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Waiter_CancelledByClient_ThrowsCancellation()
    {
        await using var holder = await Acquire(Ten, Ten.AddHours(1));
        using var cts = new CancellationTokenSource();

        var waiter = _coordinator.WaitIfOverlappingAsync(1, Ten, Ten.AddHours(1), cts.Token);
        await AssertStillWaiting([waiter]);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter.WaitAsync(Guard));
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Cleanup_ReleasesLeaseExactlyAtItsLifetime()
    {
        await using var holder = await Acquire(Ten, Ten.AddHours(1));
        var waiter = Acquire(Ten, Ten.AddHours(1));

        _time.Advance(LeaseLifetime - TimeSpan.FromSeconds(1));
        await _coordinator.CleanExpiredAsync();

        await AssertStillWaiting([waiter]);

        _time.Advance(TimeSpan.FromSeconds(1));
        await _coordinator.CleanExpiredAsync();

        await using var lease = await waiter.WaitAsync(Guard);
    }

    [Fact(Timeout = TestTimeout)]
    public async Task Cleanup_ExpiredLeaseReleasedLater_DoesNotFreeItsSuccessor()
    {
        var stale = await Acquire(Ten, Ten.AddHours(1));
        var successorTask = Acquire(Ten, Ten.AddHours(1));

        _time.Advance(LeaseLifetime);
        await _coordinator.CleanExpiredAsync();

        var successor = await successorTask.WaitAsync(Guard);

        // the forgotten holder finishes after all; the same range now belongs to the successor
        await stale.DisposeAsync();

        var next = Acquire(Ten, Ten.AddHours(1));
        await AssertStillWaiting([next]);

        await successor.DisposeAsync();
        await using var nextLease = await next.WaitAsync(Guard);
    }

    private Task<IAsyncDisposable> Acquire(DateTimeOffset start, DateTimeOffset end) =>
        _coordinator.WaitIfOverlappingAsync(LongBudget, 1, start, end, default);

    private static async Task AssertStillWaiting(IEnumerable<Task> waiters)
    {
        await Task.Delay(50);

        Assert.All(waiters, waiter => Assert.False(waiter.IsCompleted, "a waiter got through too early"));
    }
}
