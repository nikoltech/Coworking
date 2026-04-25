namespace Coworking.External.Squidex.Abstractions.Filters;

/// <summary>
/// Base path constants for Squidex content filter paths.
/// All content schema fields are nested under "data".
/// </summary>
public static class SquidexPaths
{
    /// <summary>Root prefix for all content schema field paths.</summary>
    public const string DataRoot = "data";

    /// <summary>Builds a path for an invariant (iv) field.</summary>
    public static string Iv(string fieldName) => $"{DataRoot}.{fieldName}.iv";

    /// <summary>Builds a path for a localized field.</summary>
    public static string Localized(string fieldName, string locale) =>
        $"{DataRoot}.{fieldName}.{locale}";
}