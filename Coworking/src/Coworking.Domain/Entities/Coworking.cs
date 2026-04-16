using Coworking.Domain.Common;
using Coworking.Domain.Exceptions;
using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Entities;

public class Coworking : ITrackEntity, ICanBeDisabled
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public SlotSize SlotSize { get; set; } = SlotSize.ThirtyMinutes;

    public TimeOnly OpenTime { get; set; }

    /// <summary>
    /// IANA ID
    /// </summary>
    public required string TimeZoneId { get; set; }

    public TimeOnly CloseTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DisabledAt { get; set; }

    public byte[]? Version { get; set; }

    public ICollection<Desk> Desks { get; set; } = [];

    /// <param name="timeZoneId">IANA time zone identifier</param>
    public static Coworking Create(string name, SlotSize slotSize, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Coworking name cannot be empty.");

        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new DomainException("TimeZoneId cannot be empty.");

        return new Coworking
        {
            Name = name,
            SlotSize = slotSize,
            TimeZoneId = timeZoneId
        };
    }
}