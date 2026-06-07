using Coworking.Domain.Entities;
using Coworking.Domain.Exceptions;
using System.Linq.Expressions;

namespace Coworking.Domain.Specifications;

public static class BookingSpecifications
{
    /// <summary>
    /// Can book at border times
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

    /// <summary>
    /// Represents access entitlement during coworking working hours.
    /// </summary>
    public static void ValidateAccessPeriod(DateTimeOffset start,
        DateTimeOffset end,
        Domain.Entities.Coworking coworking)
    {
        if (start >= end)
        {
            throw new DomainException(
                "Booking start time must be earlier than end time.");
        }

        if (IsNonStopWorkingHours(coworking))
        {
            return;
        }

        var startTime = TimeOnly.FromDateTime(start.DateTime);
        var endTime = TimeOnly.FromDateTime(end.DateTime);

        if (IsWithinWorkingWindow(
                startTime,
                coworking.OpenTime,
                coworking.CloseTime) is false)
        {
            throw new DomainException(
                "Booking start time is outside working hours.");
        }

        if (IsWithinWorkingWindow(
                endTime,
                coworking.OpenTime,
                coworking.CloseTime) is false)
        {
            throw new DomainException(
                "Booking end time is outside working hours.");
        }
    }

    public static bool IsWithinWorkingWindow(TimeOnly time,
        TimeOnly openTime,
        TimeOnly closeTime)
    {
        if (openTime == closeTime)
        {
            return true;
        }

        var isDaySchedule = openTime < closeTime;

        if (isDaySchedule)
        {
            return time >= openTime &&
                   time <= closeTime;
        }

        return time >= openTime ||
               time <= closeTime;
    }

    public static bool IsNonStopWorkingHours(Domain.Entities.Coworking coworking)
    {
        return coworking.OpenTime ==
               coworking.CloseTime;
    }

}