using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;

namespace Coworking.External.Squidex.Abstractions.Repository;

public interface ISquidexApiClient
{
    Task<ResponseSchema<T>> QueryAsync<T>(
        string schema, RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ResponseSchema<T>> QueryODataAsync<T>(
        string schema, ODataQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ResponseSchema<T>> QueryPostAsync<T>(
        string schema, RequestQuery query,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ResponseSchema<T>> GetByIdsAsync<T>(
        string schema, IEnumerable<string> ids,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>?> GetByIdAsync<T>(
        string schema, string id,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);

    Task<ContentDto<T>> CreateAsync<T>(
        string schema, T data, bool publish = true,
        CancellationToken ct = default);

    Task<ContentDto<T>> UpdateAsync<T>(
        string schema, string id, T data,
        CancellationToken ct = default);

    Task<ContentDto<T>> PatchAsync<T>(
        string schema, string id, T data,
        CancellationToken ct = default);

    Task DeleteAsync(
        string schema, string id, bool permanent = false,
        CancellationToken ct = default);

    Task<ContentDto<T>> ChangeStatusAsync<T>(
        string schema, string id, string newStatus,
        CancellationToken ct = default);

    Task<IReadOnlyList<SquidexLocaleInfo>> GetAppLocalesAsync(CancellationToken ct = default);

    SquidexAppOptions AppOptions { get; }
}