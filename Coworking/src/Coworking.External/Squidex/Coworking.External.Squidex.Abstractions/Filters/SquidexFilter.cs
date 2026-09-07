namespace Coworking.External.Squidex.Abstractions.Filters;

/// <summary>
/// Builds Squidex filter objects without string operation literals.
///
/// Build the path with <c>SquidexPaths</c> — never hand-write the partition suffix:
///   do:     SquidexFilter.Eq(SquidexPaths.Localized("Title", locale), "Kyiv")
///   don't:  SquidexFilter.Eq("data.Title.iv", "Kyiv")
///
/// A localized field addressed as <c>.iv</c> is rejected by Squidex with 400.
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