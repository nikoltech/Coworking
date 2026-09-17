using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using System.Linq.Expressions;

namespace Coworking.Domain.Specifications;

public static class BookingSpecifications
{
    /// <summary>
    /// Can book at border times. Time pattern is "()"
    /// </summary>
    public static Expression<Func<Booking, bool>> OverlappingWith(int deskId,
        DateTimeOffset newStart,
        DateTimeOffset newEnd)
    {
        return booking =>
            booking.DeskId == deskId &&
            booking.StartTime < newEnd &&
            booking.EndTime > newStart;
    }

    public static Expression<Func<Booking, bool>> IsActive() =>
        booking => booking.Status != BookingStatus.Cancelled &&
                   booking.Status != BookingStatus.Expired;
}
