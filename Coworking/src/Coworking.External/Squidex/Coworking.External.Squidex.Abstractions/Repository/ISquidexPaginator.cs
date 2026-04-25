using Coworking.External.Squidex.Abstractions.Models;

namespace Coworking.External.Squidex.Abstractions.Repository;

public interface ISquidexPaginator
{
    Task<ResponseSchema<T>> FetchAllAsync<T>(
        string schema,
        ISquidexApiClient client,
        RequestQuery baseQuery,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default);
}