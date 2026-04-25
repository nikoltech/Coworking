using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

public sealed record AssetDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("fileSize")] long FileSize,
    [property: JsonPropertyName("mimeType")] string MimeType,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("tags")] List<string> Tags,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("created")] DateTime Created,
    [property: JsonPropertyName("lastModified")] DateTime LastModified,
    [property: JsonPropertyName("metadata")] AssetMetadata? Metadata,
    [property: JsonPropertyName("isProtected")] bool IsProtected,
    [property: JsonPropertyName("fileHash")] string? FileHash);

public sealed record AssetMetadata(
    [property: JsonPropertyName("pixelWidth")] int? PixelWidth,
    [property: JsonPropertyName("pixelHeight")] int? PixelHeight);