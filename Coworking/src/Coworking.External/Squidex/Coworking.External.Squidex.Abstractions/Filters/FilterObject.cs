using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Filters;

public sealed record FilterObject(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("op")] string Op,
    [property: JsonPropertyName("value")] object? Value);