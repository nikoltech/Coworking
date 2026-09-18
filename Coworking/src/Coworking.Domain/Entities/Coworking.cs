using Coworking.Domain.Common;
using Coworking.Domain.Exceptions;
using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Entities;

public class Coworking : ITrackEntity, ICanBeDisabled
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SlotSize SlotSize { get; set; } = SlotSize.ThirtyMinutes;

    /// <summary>
    /// IsNonStop marks a coworking that never closes; OpenTime and CloseTime are ignored then.
    /// </summary>
    public bool IsNonStop { get; set; }

    public TimeOnly? OpenTime { get; set; }

    public TimeOnly? CloseTime { get; set; }

    /// <summary>
    /// IANA ID
    /// </summary>
    public required string TimeZoneId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DisabledAt { get; set; }

    public ICollection<Desk> Desks { get; set; } = [];

    /// <exception cref="ArgumentException">Blank name, non-IANA zone, or equal hours.</exception>
    public static Coworking CreateWithHours(
        string name, string timeZoneId, SlotSize slotSize, TimeOnly openTime, TimeOnly closeTime)
    {
        if (openTime == closeTime)
            throw new ArgumentException(
                $"Opening and closing times are equal; use {nameof(CreateNonStop)} instead.", nameof(closeTime));

        return New(name, timeZoneId, slotSize, isNonStop: false, openTime, closeTime);
    }

    /// <exception cref="ArgumentException">Blank name or non-IANA zone.</exception>
    public static Coworking CreateNonStop(string name, string timeZoneId, SlotSize slotSize) =>
        New(name, timeZoneId, slotSize, isNonStop: true, openTime: null, closeTime: null);

    private static Coworking New(
        string name,
        string timeZoneId,
        SlotSize slotSize,
        bool isNonStop,
        TimeOnly? openTime,
        TimeOnly? closeTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var timeZone = ZonedTime.Find(timeZoneId);

        return new Coworking
        {
            Name = name,
            TimeZoneId = timeZone.Id,
            SlotSize = slotSize,
            IsNonStop = isNonStop,
            OpenTime = openTime,
            CloseTime = closeTime
        };
    }
}