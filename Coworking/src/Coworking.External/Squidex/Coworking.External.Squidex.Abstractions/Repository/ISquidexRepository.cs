using Coworking.External.Squidex.Abstractions.Models;

namespace Coworking.External.Squidex.Abstractions.Repository;

/// <summary>
/// Generic read/write contract for a Squidex schema.
/// Extend in Application Ports to add schema-specific query methods.
/// </summary>
public interface ISquidexRepository<T> where T : class
{
    Task<ResponseSchema<T>> QueryAsync(
        RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>Fetches all items across all pages in parallel.</summary>
    Task<ResponseSchema<T>> GetAllAsync(
        RequestQuery? query = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>?> GetByIdAsync(
        string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>> CreateAsync(
        T data,
        bool publish = true,
        CancellationToken ct = default);

    Task<ContentDto<T>> UpdateAsync(
        string id,
        T data,
        CancellationToken ct = default);

    Task<ContentDto<T>> PatchAsync(
        string id,
        T data,
        CancellationToken ct = default);

    Task DeleteAsync(
        string id,
        bool permanent = false,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        object filter,
        bool includeUnpublished = false,
        CancellationToken ct = default);
}