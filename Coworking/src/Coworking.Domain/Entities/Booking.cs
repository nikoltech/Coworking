using Coworking.Domain.Common;
using Coworking.Domain.Enums;
using Coworking.Domain.Exceptions;
using StateMachine;

namespace Coworking.Domain.Entities;

public class Booking : ITrackEntity, IHasStateGraph<BookingStatus>
{
    public int Id { get; set; }

    public Guid AccessCode { get; set; } = Guid.NewGuid();

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Created;

    public int DeskId { get; set; }

    public Desk Desk { get; set; } = default!;


    //public Guid UserId { get; set; }

    public string UserName { get; set; } = default!;

    public string UserEmail { get; set; } = default!;

    public string? UserTimeZoneId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }


    /// <summary>
    /// Confirmed, Expired, Cancelled is final states. 
    /// Cancelled from everywhere. 
    /// Expired only after Confirmed, PendingPayment.
    /// </summary>
    public static StateGraph<BookingStatus> StateGraph { get; } =
        StateGraph<BookingStatus>.Create()
            .From(BookingStatus.Created, BookingStatus.PendingPayment, BookingStatus.Expired)
            .From(BookingStatus.PendingPayment, BookingStatus.PendingConfirmation, BookingStatus.Expired)
            .From(BookingStatus.PendingConfirmation, BookingStatus.Confirmed)
            .FromAnywhere(BookingStatus.Cancelled)
            .Build();

    private readonly Lifecycle<BookingStatus> _stateLifecycle = new(BookingStatus.Created, StateGraph);


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

    public void SetStatus(BookingStatus newStatus)
    {
        _stateLifecycle.MoveTo(newStatus);

        Status = newStatus;
    }
}
