using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Client;

namespace Coworking.External.Squidex.Repository;

/// <summary>
/// Base implementation for Squidex Assets repository.
/// Inherit to add project-specific asset methods.
/// </summary>
public class SquidexAssetRepository(SquidexAssetClient client)
    : ISquidexAssetRepository
{
    protected readonly SquidexAssetClient Client = client;

    public Task<ResponseSchema<AssetDto>> QueryAsync(
        AssetQuery? query = null,
        CancellationToken ct = default) =>
        Client.QueryAsync(query, ct);

    public Task<AssetDto?> GetByIdAsync(
        string id, CancellationToken ct = default) =>
        Client.GetByIdAsync(id, ct);

    public Task<AssetDto> UploadAsync(
        Stream stream, string fileName, string mimeType, CancellationToken ct = default) =>
        Client.UploadAsync(stream, fileName, mimeType, ct);

    public Task<AssetDto> UpdateMetadataAsync(
        string id, UpdateAssetRequest request, CancellationToken ct = default) =>
        Client.UpdateMetadataAsync(id, request, ct);

    public Task DeleteAsync(
        string id, bool permanent = false, CancellationToken ct = default) =>
        Client.DeleteAsync(id, permanent, ct);
}