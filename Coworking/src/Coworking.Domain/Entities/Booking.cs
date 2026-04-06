using Coworking.Domain.Common;
using Coworking.Domain.Exceptions;

namespace Coworking.Domain.Entities;

public class Booking : ITrackEntity
{
    public Guid Id { get; set; }

    public Guid DeskId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public string TimeZoneId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public static Booking Create(Guid deskId, Guid userId, DateTimeOffset startTime, DateTimeOffset endTime, string timeZoneId)
    {
        if (startTime >= endTime)
            throw new DomainException("The start time must be before the end time.");

        if (startTime < DateTimeOffset.UtcNow)
            throw new DomainException("Cannot book in the past.");

        return new Booking
        {
            DeskId = deskId,
            UserId = userId,
            StartTime = startTime,
            EndTime = endTime,
            TimeZoneId = timeZoneId
        };
    }

}
