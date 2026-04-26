namespace Coworking.External.Squidex.Abstractions.Filters;

/// <summary>
/// Path builders for Squidex filter expressions.
///
/// IMPORTANT — path separator depends on query mode:
///   JSON query (q= param or POST body): dot separator  → data.Title.iv
///   OData ($filter, $orderby):          slash separator → data/Title/iv
///
/// Use JsonPath for RequestQuery (default).
/// Use ODataPath only when building raw OData strings.
/// </summary>
public static class SquidexPaths
{
    public const string DataRoot = "data";

    // ── JSON paths (dot separator) — use with RequestQuery ───────────────────

    /// <summary>Invariant field path for JSON query. Example: data.Title.iv</summary>
    public static string Iv(string fieldName) =>
        $"{DataRoot}.{fieldName}.iv";

    /// <summary>Localized field path for JSON query. Example: data.Title.uk-UA</summary>
    public static string Localized(string fieldName, string locale) =>
        $"{DataRoot}.{fieldName}.{locale}";

    // ── OData paths (slash separator) — use with raw OData strings ───────────

    /// <summary>Invariant field path for OData. Example: data/Title/iv</summary>
    public static string ODataIv(string fieldName) =>
        $"{DataRoot}/{fieldName}/iv";

    /// <summary>Localized field path for OData. Example: data/Title/uk-UA</summary>
    public static string ODataLocalized(string fieldName, string locale) =>
        $"{DataRoot}/{fieldName}/{locale}";
}