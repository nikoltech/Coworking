using Coworking.Domain.Common;
using Coworking.Domain.Exceptions;
using Coworking.Domain.ValueObjects;

namespace Coworking.Domain.Entities;

public class Booking : ITrackEntity
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public string? UserTimeZoneId { get; set; }

    public int DeskId { get; set; }

    public Desk Desk { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public static Booking Create(
        int deskId,
        Guid userId,
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        var utcStartTime = startTime.ToUniversalTime();
        var utcEndTime = endTime.ToUniversalTime();

        if (utcStartTime >= utcEndTime)
            throw new DomainException("The start time must be before the end time.");

        if (utcStartTime < DateTimeOffset.UtcNow)
            throw new DomainException("Cannot book in the past.");

        return new Booking
        {
            DeskId = deskId,
            UserId = userId,
            StartTime = utcStartTime,
            EndTime = utcEndTime
        };
    }

}
