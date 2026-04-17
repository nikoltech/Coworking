using Coworking.Domain.Exceptions;

namespace Coworking.Domain.ValueObjects;

public sealed record SlotSize
{
    public static readonly SlotSize TenMinutes = new(10);
    public static readonly SlotSize FifteenMinutes = new(15);
    public static readonly SlotSize ThirtyMinutes = new(30);
    public static readonly SlotSize SixtyMinutes = new(60);

    const int BaseStepInMinutes = 5;

    public int Minutes { get; }

    public TimeSpan Value => TimeSpan.FromMinutes(Minutes);

    private SlotSize(int minutes) => Minutes = minutes;


    public static SlotSize From(int minutes)
    {
        if (minutes <= 0)
            throw new DomainException("Slot size must be positive.");

        if (minutes % BaseStepInMinutes != 0)
            throw new DomainException($"Slot size must be a multiple of {BaseStepInMinutes}");

        return minutes switch
        {
            // slot size in order of prevalence
            60 => SixtyMinutes,
            15 => FifteenMinutes,
            30 => ThirtyMinutes,
            10 => TenMinutes,
            _ => new SlotSize(minutes)
        };
    }
}