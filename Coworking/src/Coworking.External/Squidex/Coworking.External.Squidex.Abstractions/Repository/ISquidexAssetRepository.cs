using Coworking.External.Squidex.Abstractions.Models;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Repository;

public interface ISquidexAssetRepository
{
    Task<ResponseSchema<AssetDto>> QueryAsync(
        AssetQuery? query = null,
        CancellationToken ct = default);

    Task<AssetDto?> GetByIdAsync(
        string id,
        CancellationToken ct = default);

    Task<AssetDto> UploadAsync(
        Stream stream,
        string fileName,
        string mimeType,
        CancellationToken ct = default);

    Task<AssetDto> UpdateMetadataAsync(
        string id,
        UpdateAssetRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(
        string id,
        bool permanent = false,
        CancellationToken ct = default);
}

public sealed record UpdateAssetRequest(
    [property: JsonPropertyName("fileName")] string? FileName = null,
    [property: JsonPropertyName("tags")] List<string>? Tags = null,
    [property: JsonPropertyName("isProtected")] bool? IsProtected = null,
    [property: JsonPropertyName("metadata")] object? Metadata = null);