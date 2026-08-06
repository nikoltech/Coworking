using Coworking.Infrastructure.Synchronization.InMemory;

namespace Coworking.UnitTests;

/// <summary>
/// Guards the per-desk lane split: desks must stay independent, and a desk must still
/// serialize overlapping ranges within itself.
/// </summary>
public class BookingAccessCoordinatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddHours(1);

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task DifferentDesks_DoNotWaitOnEachOther()
    {
        var coordinator = new InMemoryBookingAccessCoordinator();

        await using var first = await coordinator.WaitIfOverlappingAsync(1, Start, End, default);

        // same range, different desk: must be granted while the first lease is still held
        var second = coordinator.WaitIfOverlappingAsync(2, Start, End, default);

        var granted = await Task.WhenAny(second, Task.Delay(Timeout));

        Assert.Same(second, granted);
        await using var _ = await second;
    }

    [Fact]
    public async Task SameDeskOverlappingRange_WaitsForTheHolder()
    {
        var coordinator = new InMemoryBookingAccessCoordinator();

        var first = await coordinator.WaitIfOverlappingAsync(1, Start, End, default);

        var second = coordinator.WaitIfOverlappingAsync(1, Start.AddMinutes(30), End.AddMinutes(30), default);

        var raced = await Task.WhenAny(second, Task.Delay(200));
        Assert.NotSame(second, raced);

        await first.DisposeAsync();

        await using var _ = await second.WaitAsync(Timeout);
    }

    [Fact]
    public async Task SameDeskNonOverlappingRange_IsGrantedImmediately()
    {
        var coordinator = new InMemoryBookingAccessCoordinator();

        await using var first = await coordinator.WaitIfOverlappingAsync(1, Start, End, default);

        var second = coordinator.WaitIfOverlappingAsync(1, End, End.AddHours(1), default);

        var granted = await Task.WhenAny(second, Task.Delay(Timeout));

        Assert.Same(second, granted);
        await using var _ = await second;
    }

    [Fact]
    public async Task ManyDesks_AreAllGrantedConcurrently()
    {
        var coordinator = new InMemoryBookingAccessCoordinator();

        var leases = await Task.WhenAll(
            Enumerable.Range(1, 50)
                .Select(deskId => coordinator.WaitIfOverlappingAsync(deskId, Start, End, default)))
            .WaitAsync(Timeout);

        Assert.Equal(50, leases.Length);

        foreach (var lease in leases)
            await lease.DisposeAsync();
    }
}
