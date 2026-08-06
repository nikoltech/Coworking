namespace Coworking.Domain.Common;

public static class ZonedTime
{
    /// <summary>
    /// Reads a wall-clock label as a moment. Unlike ConvertTimeToUtc, tolerates a reading
    /// the spring-forward transition skipped — GetUtcOffset always answers.
    /// </summary>
    public static DateTimeOffset FromWallClock(DateTime local, TimeZoneInfo timeZone) =>
        new(local, timeZone.GetUtcOffset(local));
}
