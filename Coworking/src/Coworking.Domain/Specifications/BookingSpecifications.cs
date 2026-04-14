using Coworking.Domain.Entities;
using System.Linq.Expressions;

namespace Coworking.Domain.Specifications;

public static class BookingSpecifications
{
    /// <summary>
    /// Can book at border times
    /// </summary>
    public static Expression<Func<Booking, bool>> OverlappingWith(int deskId, DateTimeOffset newStart, DateTimeOffset newEnd)
    {
        return booking =>
            booking.DeskId == deskId &&
            booking.StartTime < newEnd &&
            booking.EndTime > newStart;
    }

    /// <summary>
    /// Supports 24/7 coworking (open == close) and midnight crossing hours
    /// </summary>
    public static bool IsWithinWorkingHours(DateTimeOffset start, DateTimeOffset end, TimeOnly coworkingOpenTime, TimeOnly coworkingCloseTime)
    {
        // for 24/7
        if (coworkingOpenTime == coworkingCloseTime)
            return true;

        var startT = TimeOnly.FromDateTime(start.DateTime);
        var endT = TimeOnly.FromDateTime(end.DateTime);
        var open = coworkingOpenTime;
        var close = coworkingCloseTime;

        // daytime hours (e.g. 08:00 - 18:00)
        if (open < close)
            return startT >= open && endT <= close;

        // midnight crossing open > close (e.g. 22:00 - 06:00)
        return (startT >= open || startT < close)
            && (endT > open || endT <= close);
    }
}