using Coworking.Domain.Common;
using Coworking.Domain.Enums;
using Coworking.Domain.Exceptions;

namespace Coworking.Domain.Entities;

public class Booking : ITrackEntity
{
    public int Id { get; set; }

    public Guid AccessCode { get; set; } = Guid.NewGuid();

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public BookingStatus Status { get; set; }

    public int DeskId { get; set; }

    public Desk Desk { get; set; } = default!;


    //public Guid UserId { get; set; }

    public string UserName { get; set; } = default!;

    public string UserEmail { get; set; } = default!;

    public string? UserTimeZoneId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public static Booking Create(int deskId,
        string userName, // ValueObject? depend of choosen user design
        string userEmail,  // ValueObject?
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
            AccessCode = Guid.NewGuid(),
            DeskId = deskId,
            UserName = userName,
            UserEmail = userEmail,
            StartTime = utcStartTime,
            EndTime = utcEndTime
        };
    }

    public void SetStatus(BookingStatus pendingPayment)
    {
        if (Status == BookingStatus.Cancelled || Status == BookingStatus.Expired)
            throw new DomainException("Cannot change status of a cancelled or expired booking.");

        Status = pendingPayment;
    }
}
