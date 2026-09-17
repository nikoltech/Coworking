using Coworking.Domain.Common;
using Coworking.Domain.Exceptions;

namespace Coworking.Domain.ValueObjects;

/// <summary>
/// WorkingSchedule turns a coworking's time zone and hours into real moments.
/// Handlers work with coworking time through it, not through TimeZoneInfo.
/// </summary>
public sealed class WorkingSchedule
{
    private readonly TimeZoneInfo _timeZone;
    private readonly TimeOnly _open;
    private readonly TimeOnly _close;

    private WorkingSchedule(TimeZoneInfo timeZone, SlotSize slotSize, bool isNonStop, TimeOnly open, TimeOnly close)
    {
        _timeZone = timeZone;
        _open = open;
        _close = close;
        SlotSize = slotSize;
        IsNonStop = isNonStop;
    }

    /// <summary>
    /// For builds the schedule of a coworking.
    /// Throws DomainException when the time zone is unknown or the hours are invalid.
    /// </summary>
    public static WorkingSchedule For(Entities.Coworking coworking)
    {
        var timeZone = FindTimeZone(coworking.TimeZoneId);

        if (coworking.IsNonStop)
            return new(timeZone, coworking.SlotSize, isNonStop: true, TimeOnly.MinValue, TimeOnly.MinValue);

        if (coworking.OpenTime is not { } open || coworking.CloseTime is not { } close)
            throw new DomainException($"Coworking '{coworking.Name}' has no working hours.");

        if (open == close)
            throw new DomainException(
                $"Coworking '{coworking.Name}' opens and closes at the same time; mark it as non-stop instead.");

        return new(timeZone, coworking.SlotSize, isNonStop: false, open, close);
    }

    /// <summary>IsNonStop is true when the coworking never closes; each local day is then one window.</summary>
    public bool IsNonStop { get; }

    /// <summary>SlotSize is the booking step; the step grid starts anew at each window's opening.</summary>
    public SlotSize SlotSize { get; }

    /// <summary>
    /// WindowForStart returns the window a booking starting at this moment belongs to,
    /// or null when the coworking is closed then. Closing time itself is not open.
    /// </summary>
    public WorkingWindow? WindowForStart(DateTimeOffset start) =>
        FindWindow(start, w => w.Start <= start && start < w.End);

    /// <summary>
    /// WindowForEnd returns the window a booking ending at this moment belongs to,
    /// or null when the coworking is closed then. A booking may end exactly at closing.
    /// </summary>
    public WorkingWindow? WindowForEnd(DateTimeOffset end) =>
        FindWindow(end, w => w.Start < end && end <= w.End);

    /// <summary>
    /// WindowsBetween returns one working window per local day, in real length:
    /// a DST day gives 23 or 25 hours, not 24.
    /// </summary>
    public IReadOnlyList<WorkingWindow> WindowsBetween(DateOnly from, DateOnly to)
    {
        var windows = new List<WorkingWindow>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var window = WindowOn(date);

            if (window.Start < window.End)
                windows.Add(window);
        }

        return windows;
    }

    /// <summary>
    /// QueryBoundaries returns the moments to load bookings for these local days.
    /// Includes the next day, where a night window ends.
    /// </summary>
    public (DateTimeOffset Start, DateTimeOffset End) QueryBoundaries(DateOnly from, DateOnly to) =>
        (ZonedTime.FromWallClock(from.ToDateTime(TimeOnly.MinValue), _timeZone),
         ZonedTime.FromWallClock(to.AddDays(2).ToDateTime(TimeOnly.MinValue), _timeZone));

    /// <summary>
    /// EnsureWithinWorkingHours throws DomainException unless both start and end fall inside
    /// working windows. Closed hours between them are allowed: a booking may span several days.
    /// </summary>
    public void EnsureWithinWorkingHours(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
            throw new DomainException("Booking start time must be earlier than end time.");

        if (WindowForStart(start) is null)
            throw new DomainException("Booking start time is outside working hours.");

        if (WindowForEnd(end) is null)
            throw new DomainException("Booking end time is outside working hours.");
    }

    /// <summary>ToLocal labels the moment with the coworking's offset, for display.</summary>
    public DateTimeOffset ToLocal(DateTimeOffset moment) =>
        TimeZoneInfo.ConvertTime(moment, _timeZone);

    /// <summary>
    /// BillableTime returns the open time inside the period, which is what the customer pays for.
    /// Not implemented until payments exist.
    /// </summary>
    public TimeSpan BillableTime(DateTimeOffset start, DateTimeOffset end) =>
        throw new NotImplementedException("Billing is not implemented yet.");

    private WorkingWindow? FindWindow(DateTimeOffset moment, Func<WorkingWindow, bool> contains)
    {
        var localDate = DateOnly.FromDateTime(ToLocal(moment).DateTime);

        // a moment belongs to the window opened on its local day or, for night and non-stop hours, the day before
        foreach (var date in new[] { localDate.AddDays(-1), localDate })
        {
            var window = WindowOn(date);

            if (contains(window))
                return window;
        }

        return null;
    }

    private WorkingWindow WindowOn(DateOnly date)
    {
        var closesNextDay = IsNonStop || _close < _open;
        var closingDate = closesNextDay ? date.AddDays(1) : date;

        return new(
            ZonedTime.FromWallClock(date.ToDateTime(_open), _timeZone),
            ZonedTime.FromWallClock(closingDate.ToDateTime(_close), _timeZone));
    }

    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new DomainException($"Unknown time zone '{timeZoneId}'.", ex);
        }
    }
}
