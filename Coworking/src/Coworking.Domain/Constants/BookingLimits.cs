namespace Coworking.Domain.Constants;

public static class BookingLimits
{
    /// RFC 5321: 64 for the local part, 255 for the domain, one separator.
    public const int UserEmailMaxLength = 320;

    /// Room for a full name in any script, patronymic and transliteration included.
    public const int UserNameMaxLength = 200;

    public const int UserTimeZoneMaxLength = 100;
}
