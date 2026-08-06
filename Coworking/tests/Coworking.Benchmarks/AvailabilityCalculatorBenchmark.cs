using BenchmarkDotNet.Attributes;
using Coworking.Domain.Services.Availability;

namespace Coworking.Benchmarks;

/// <summary>
/// The endpoint allows a 90-day range, so the cost of walking `busy` once per day is
/// what this measures.
/// </summary>
[MemoryDiagnoser]
public class AvailabilityCalculatorBenchmark
{
    private readonly IAvailabilityCalculator _calculator = new AvailabilityCalculator();
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");

    private DateOnly _from;
    private DateOnly _to;
    private (DateTimeOffset Start, DateTimeOffset End)[] _busy = [];

    [Params(500)]
    public int BookingCount { get; set; }

    [Params(1, 30, 90)]
    public int Days { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _from = new DateOnly(2026, 6, 1);
        _to = _from.AddDays(Days - 1);

        // spread across the range, shuffled so the input is not already ordered
        var random = new Random(42);
        var offset = TimeSpan.FromHours(3);

        _busy = Enumerable.Range(0, BookingCount)
            .Select(_ =>
            {
                var day = _from.AddDays(random.Next(Days));
                var hour = random.Next(0, 22);
                var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(hour, 0)), offset);

                return (start, start.AddHours(1));
            })
            .ToArray();
    }

    [Benchmark(Description = "24/7 schedule")]
    public int NonStop() =>
        _calculator.Calculate(_from, _to, new TimeOnly(0, 0), new TimeOnly(0, 0), Kyiv, _busy).Count;

    [Benchmark(Description = "08:00-20:00 schedule")]
    public int DaySchedule() =>
        _calculator.Calculate(_from, _to, new TimeOnly(8, 0), new TimeOnly(20, 0), Kyiv, _busy).Count;
}
