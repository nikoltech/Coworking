using Coworking.Domain.Exceptions;

namespace Coworking.Domain.ValueObjects;

public sealed record SlotSize
{
    // Properties (not fields) so each access returns a new instance.
    // OwnsOne requires a distinct CLR object per owner; record equality remains structural.
    public static SlotSize TenMinutes => new(10);
    public static SlotSize FifteenMinutes => new(15);
    public static SlotSize ThirtyMinutes => new(30);
    public static SlotSize SixtyMinutes => new(60);

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

        return new SlotSize(minutes);
    }
}