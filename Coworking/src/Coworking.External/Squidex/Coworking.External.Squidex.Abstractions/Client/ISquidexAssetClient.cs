using Coworking.External.Squidex.Abstractions.Models;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Client;

/// <summary>
/// Low-level client for the Squidex Assets API.
/// Separate from <see cref="ISquidexApiClient"/> — different endpoint and response shape
/// (assets are flat, not schema content wrapped in a data envelope).
/// </summary>
public interface ISquidexAssetClient
{
    Task<AssetsResponse> QueryAsync(
        AssetQuery? query = null,
        CancellationToken ct = default);

    Task<AssetDto?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<AssetDto> UploadAsync(Stream stream, string fileName,
        string mimeType,
        CancellationToken ct = default);

    Task<AssetDto> UpdateMetadataAsync(
        string id,
        UpdateAssetRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(string id,
        bool permanent = false,
        CancellationToken ct = default);
}

public sealed record UpdateAssetRequest(
    [property: JsonPropertyName("fileName")] string? FileName = null,
    [property: JsonPropertyName("tags")] List<string>? Tags = null,
    [property: JsonPropertyName("isProtected")] bool? IsProtected = null,
    [property: JsonPropertyName("metadata")] object? Metadata = null);
