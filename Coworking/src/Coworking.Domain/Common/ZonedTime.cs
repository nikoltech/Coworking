namespace Coworking.Domain.Common;

public static class ZonedTime
{
    /// <summary>
    /// Reads a wall-clock label as a moment. Unlike ConvertTimeToUtc, tolerates a reading
    /// the spring-forward transition skipped — GetUtcOffset always answers.
    /// </summary>
    public static DateTimeOffset FromWallClock(DateTime local, TimeZoneInfo timeZone) =>
        new(local, timeZone.GetUtcOffset(local));

    /// <summary>
    /// Find resolves an IANA time zone id. Windows ids are rejected: they resolve differently
    /// depending on the host, so stored data would stop being portable.
    /// </summary>
    /// <exception cref="ArgumentException">Blank, unknown, or a Windows id.</exception>
    public static TimeZoneInfo Find(string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);

        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ArgumentException($"Unknown time zone '{timeZoneId}'.", nameof(timeZoneId), ex);
        }

        if (timeZone.HasIanaId)
            return timeZone;

        var suggestion = TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var iana)
            ? $", for example '{iana}'"
            : string.Empty;

        throw new ArgumentException(
            $"Time zone '{timeZoneId}' is not an IANA id{suggestion}.", nameof(timeZoneId));
    }
}
