using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Fluent builder for Squidex JSON query param (?q=...).
/// Serialized as JSON and URL-encoded. Overrides all OData parameters.
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

public sealed record SortOption(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("order")] string Order)
{
    public static SortOption Asc(string path) => new(path, "ascending");
    public static SortOption Desc(string path) => new(path, "descending");
}