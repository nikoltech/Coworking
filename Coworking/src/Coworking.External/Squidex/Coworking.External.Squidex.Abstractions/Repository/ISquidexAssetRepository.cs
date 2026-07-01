using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;

namespace Coworking.External.Squidex.Abstractions.Repository;

public interface ISquidexAssetRepository
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
