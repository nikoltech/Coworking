using Coworking.Domain.Exceptions;

namespace Coworking.Domain.ValueObjects;

public sealed record SlotSize
{
    public static readonly SlotSize TenMinutes = new(10);
    public static readonly SlotSize FifteenMinutes = new(15);
    public static readonly SlotSize ThirtyMinutes = new(30);
    public static readonly SlotSize SixtyMinutes = new(60);

    public int Minutes { get; }
    public TimeSpan Value => TimeSpan.FromMinutes(Minutes);

    private SlotSize(int minutes) => Minutes = minutes;

    public static SlotSize From(int minutes)
    {
        if (minutes <= 0)
            throw new DomainException("Slot size must be positive.");

        if (60 % minutes != 0)
            throw new DomainException("Slot size must be a divisor of 60 (e.g. 10, 15, 20, 30, 60).");

        return minutes switch
        {
            15 => FifteenMinutes, // More common slot size
            30 => ThirtyMinutes,  // More common slot size
            10 => TenMinutes,
            60 => SixtyMinutes,
            _ => new SlotSize(minutes)
        };
    }
}