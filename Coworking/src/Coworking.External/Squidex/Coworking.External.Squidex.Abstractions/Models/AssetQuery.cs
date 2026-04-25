namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Query parameters for Squidex Assets API.
/// Different from content queries — no JSON q param, uses OData directly.
/// </summary>
public sealed class AssetQuery
{
    public int? Top { get; set; }
    public int Skip { get; set; }
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
    public List<string>? Tags { get; set; }
    public string? FullText { get; set; }

    public static AssetQuery Create() => new();

    public AssetQuery WithTop(int top) { Top = top; return this; }
    public AssetQuery WithSkip(int skip) { Skip = skip; return this; }
    public AssetQuery WithFilter(string filter) { Filter = filter; return this; }
    public AssetQuery WithTags(List<string> tags) { Tags = tags; return this; }
    public AssetQuery WithFullText(string text) { FullText = text; return this; }
}