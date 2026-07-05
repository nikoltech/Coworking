using Coworking.External.Squidex.Abstractions.Models;

namespace Coworking.External.Squidex.Abstractions.Set;

public interface ISquidexSet<T> where T : class
{
    Task<ResponseSchema<T>> QueryAsync(RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>OData query — slash path separator (data/Title/iv).</summary>
    Task<ResponseSchema<T>> QueryODataAsync(ODataQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>POST body query — avoids URL length limit for complex queries.</summary>
    Task<ResponseSchema<T>> QueryPostAsync(RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ResponseSchema<T>> GetAllAsync(RequestQuery? query = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>Fetch by IDs — batched at 80 IDs per request.</summary>
    Task<ResponseSchema<T>> GetByIdsAsync(IEnumerable<string> ids,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>?> GetByIdAsync(string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets content by ID, returning a flag indicating if the content has not been modified since the known version.
    /// </summary>
    /// <param name="knownVersion">Optional ETag for conditional GET.</param>
    /// <returns>If content is not modified, returns NotModified=true and null content. Otherwise, returns content with NotModified=false.</returns>
    Task<(ContentDto<T>? Content, bool NotModified)> GetByIdConditionalAsync(string id,
        int? knownVersion = null,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>> CreateAsync(T data,
        bool publish = true,
        CancellationToken ct = default);

    Task<ContentDto<T>> UpdateAsync(string id, T data,
        int? expectedVersion = null,
        CancellationToken ct = default);

    Task<ContentDto<T>> PatchAsync(string id, T data,
        int? expectedVersion = null,
        CancellationToken ct = default);

    Task DeleteAsync(string id,
        bool permanent = false,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(object filter,
        bool includeUnpublished = false,
        CancellationToken ct = default);
}