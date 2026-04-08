using Coworking.Domain.Entities;
using System.Linq.Expressions;

public static class BookingSpecifications
{
    /// <summary>
    /// Can book at border times
    /// </summary>
    public static Expression<Func<Booking, bool>> OverlappingWith(Guid deskId, DateTimeOffset newStart, DateTimeOffset newEnd)
    {
        return booking =>
            booking.DeskId == deskId &&
            booking.StartTime < newEnd &&
            booking.EndTime > newStart;
    }
}