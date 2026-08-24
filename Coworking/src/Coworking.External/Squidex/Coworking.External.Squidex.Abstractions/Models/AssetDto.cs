using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

public sealed record AssetDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("fileSize")] long FileSize,
    [property: JsonPropertyName("mimeType")] string MimeType,
    [property: JsonPropertyName("url")] string Url,
    List<string>? Tags,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("created")] DateTime Created,
    [property: JsonPropertyName("lastModified")] DateTime LastModified,
    [property: JsonPropertyName("metadata")] AssetMetadata? Metadata,
    [property: JsonPropertyName("isProtected")] bool IsProtected,
    [property: JsonPropertyName("fileHash")] string? FileHash)
{
    [JsonPropertyName("tags")]
    public List<string> Tags { get; } = Tags ?? [];
}

public sealed record AssetMetadata(
    [property: JsonPropertyName("pixelWidth")] int? PixelWidth,
    [property: JsonPropertyName("pixelHeight")] int? PixelHeight);

/// <summary>
/// Response shape for the Squidex Assets query endpoint.
/// Assets are returned flat — not wrapped in <see cref="ContentDto{T}"/> like schema content.
/// </summary>
public sealed record AssetsResponse(
    [property: JsonPropertyName("total")] long Total,
    List<AssetDto>? Items)
{
    // an absent or explicitly null array both mean "no items" — never hand out null
    [JsonPropertyName("items")]
    public List<AssetDto> Items { get; } = Items ?? [];
}