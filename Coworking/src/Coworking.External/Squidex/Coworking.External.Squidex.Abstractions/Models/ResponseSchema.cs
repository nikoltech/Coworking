using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

public sealed record ResponseSchema<T>(
    [property: JsonPropertyName("total")] long Total,
    List<ContentDto<T>>? Items)
{
    // an absent or explicitly null array both mean "no items" — never hand out null
    [JsonPropertyName("items")]
    public List<ContentDto<T>> Items { get; } = Items ?? [];
}
