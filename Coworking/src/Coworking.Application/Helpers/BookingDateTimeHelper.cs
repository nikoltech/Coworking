using System.Globalization;

namespace Coworking.Application.Helpers;

public static class BookingDateTimeHelper
{
    public static string FormatDate(DateTimeOffset dt, string timeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(dt, zone);
        return local.ToString("dddd, MMMM d yyyy · HH:mm", CultureInfo.InvariantCulture);
    }
}