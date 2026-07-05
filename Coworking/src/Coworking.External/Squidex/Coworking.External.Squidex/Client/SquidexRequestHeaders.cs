namespace Coworking.External.Squidex.Client;

/// <summary>
/// Custom request headers Squidex reads to control query behavior (see QueryOptions).
/// </summary>
public static class SquidexRequestHeaders
{
    /// <summary>Includes unpublished content in the response.</summary>
    public const string Unpublished = "X-Unpublished";

    /// <summary>Skips the exact total count for faster paging.</summary>
    public const string NoSlowTotal = "X-NoSlowTotal";

    /// <summary>Returns fields without the locale wrapper.</summary>
    public const string Flatten = "X-Flatten";

    /// <summary>Restricts the localized fields returned to these locales.</summary>
    public const string Languages = "X-Languages";
}
