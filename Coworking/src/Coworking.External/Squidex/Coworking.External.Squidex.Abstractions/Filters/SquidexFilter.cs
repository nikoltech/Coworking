using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Filters;

/// <summary>
/// Builds Squidex filter objects without string operation literals.
///
/// Always use path constants from *Paths.cs per schema — never raw strings:
///   SquidexFilter.Eq(CityPaths.Title, "Kyiv")   ✅
///   SquidexFilter.Eq("data.Title.iv", "Kyiv")   ❌
///
/// Path constants are the single source of truth — update there and
/// all filters update automatically.
/// </summary>
public static class SquidexFilter
{
    public static FilterObject Eq(string path, object value) => new(path, "eq", value);
    public static FilterObject Ne(string path, object value) => new(path, "ne", value);
    public static FilterObject Gt(string path, object value) => new(path, "gt", value);
    public static FilterObject Lt(string path, object value) => new(path, "lt", value);
    public static FilterObject Ge(string path, object value) => new(path, "ge", value);
    public static FilterObject Le(string path, object value) => new(path, "le", value);

    public static FilterObject In(string path, IEnumerable<string> values) =>
        new(path, "in", values);

    public static FilterObject Contains(string path, string value) =>
        new(path, "contains", value);

    public static FilterObject StartsWith(string path, string value) =>
        new(path, "startsWith", value);

    public static FilterObject Empty(string path) => new(path, "empty", null);
    public static FilterObject Exists(string path) => new(path, "exists", null);

    public static FilterLogical And(params object[] filters) => new("and", filters);
    public static FilterLogical Or(params object[] filters) => new("or", filters);
}

public sealed record FilterObject(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("op")] string Op,
    [property: JsonPropertyName("value")] object? Value);

public sealed class FilterLogical(string op, object[] filters)
    : Dictionary<string, object[]>
{{ Add(op, filters); }}