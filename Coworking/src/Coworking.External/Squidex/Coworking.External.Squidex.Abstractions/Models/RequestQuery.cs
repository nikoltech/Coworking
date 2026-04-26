using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Squidex query builder.
/// Serialized as JSON and passed as ?q= param (GET) or POST body.
/// Path separator: dot (data.Title.iv).
/// </summary>
public sealed class RequestQuery
{
    [JsonPropertyName("take")] public int? Take { get; set; }
    [JsonPropertyName("skip")] public int Skip { get; set; }
    [JsonPropertyName("filter")] public object? Filter { get; set; }
    [JsonPropertyName("sort")] public List<SortOption>? Sort { get; set; }
    [JsonPropertyName("fullText")] public string? FullText { get; set; }

    public static RequestQuery Create() => new();

    public RequestQuery WithTake(int take) { Take = take; return this; }
    public RequestQuery WithSkip(int skip) { Skip = skip; return this; }
    public RequestQuery WithFilter(object filter) { Filter = filter; return this; }
    public RequestQuery WithSort(List<SortOption> sort) { Sort = sort; return this; }
    public RequestQuery WithFullText(string text) { FullText = text; return this; }
}

/// <summary>
/// Raw OData query string. Path separator: slash (data/Title/iv).
/// Use only when RequestQuery is not expressive enough.
/// </summary>
public sealed class ODataQuery
{
    public int? Top { get; set; }
    public int Skip { get; set; }
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
    public string? Search { get; set; }

    public static ODataQuery Create() => new();

    public ODataQuery WithTop(int top) { Top = top; return this; }
    public ODataQuery WithSkip(int skip) { Skip = skip; return this; }
    public ODataQuery WithFilter(string filter) { Filter = filter; return this; }
    public ODataQuery WithOrderBy(string ob) { OrderBy = ob; return this; }
    public ODataQuery WithSearch(string search) { Search = search; return this; }
}

public sealed record SortOption(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("order")] string Order)
{
    public static SortOption Asc(string path) => new(path, "ascending");
    public static SortOption Desc(string path) => new(path, "descending");
}