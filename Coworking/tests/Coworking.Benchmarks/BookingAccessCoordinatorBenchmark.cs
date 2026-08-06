using BenchmarkDotNet.Attributes;
using Coworking.Infrastructure.Synchronization.InMemory;

namespace Coworking.Benchmarks;

/// <summary>
/// Concurrent acquire/release across distinct desks — the case where a single global
/// lock turns independent bookings into a queue.
/// </summary>
[MemoryDiagnoser]
public class BookingAccessCoordinatorBenchmark
{
    private static readonly DateTimeOffset Start = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddHours(1);

    private InMemoryBookingAccessCoordinator _coordinator = null!;

    [Params(10, 100)]
    public int DeskCount { get; set; }

    [IterationSetup]
    public void Setup() => _coordinator = new InMemoryBookingAccessCoordinator();

    [Benchmark(Description = "concurrent acquire/release, one range per desk")]
    public async Task AcquireAndRelease()
    {
        var tasks = new Task[DeskCount];

        for (var i = 0; i < DeskCount; i++)
        {
            var deskId = i + 1;

            tasks[i] = Task.Run(async () =>
            {
                await using var lease =
                    await _coordinator.WaitIfOverlappingAsync(deskId, Start, End, CancellationToken.None);
            });
        }

        await Task.WhenAll(tasks);
    }
}
