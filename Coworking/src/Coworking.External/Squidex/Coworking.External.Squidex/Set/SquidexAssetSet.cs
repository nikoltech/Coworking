using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Set;

namespace Coworking.External.Squidex.Set;

/// <summary>
/// Base implementation for the Squidex Assets set.
/// Inherit to add project-specific asset methods.
/// </summary>
public class SquidexAssetSet(ISquidexAssetClient client)
    : ISquidexAssetSet
{
    protected readonly ISquidexAssetClient Client = client;

    public Task<AssetsResponse> QueryAsync(
        AssetQuery? query = null,
        CancellationToken ct = default) =>
        Client.QueryAsync(query, ct);

    public Task<AssetDto?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Client.GetByIdAsync(id, ct);

    public Task<AssetDto> UploadAsync(Stream stream, string fileName,
        string mimeType,
        CancellationToken ct = default) =>
        Client.UploadAsync(stream, fileName, mimeType, ct);

    public Task<AssetDto> UpdateMetadataAsync(
        string id,
        UpdateAssetRequest request,
        CancellationToken ct = default) =>
        Client.UpdateMetadataAsync(id, request, ct);

    public Task DeleteAsync(string id,
        bool permanent = false,
        CancellationToken ct = default) =>
        Client.DeleteAsync(id, permanent, ct);
}
