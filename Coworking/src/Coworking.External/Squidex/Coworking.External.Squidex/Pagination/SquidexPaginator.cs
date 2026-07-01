using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Repository;

namespace Coworking.External.Squidex.Pagination;

/// <summary>
/// Fetches all pages of a Squidex query in parallel.
/// pageSize is passed per-call from AppOptions to support multiple apps.
/// </summary>
public sealed class SquidexPaginator : ISquidexPaginator
{
    public async Task<ResponseSchema<T>> FetchAllAsync<T>(string schema, ISquidexApiClient client,
        RequestQuery baseQuery,
        int pageSize,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        baseQuery.Take = pageSize;
        baseQuery.Skip = 0;

        var firstPage = await client.QueryAsync<T>(schema, baseQuery, queryOptions, ct);

        if (firstPage.Total <= pageSize)
            return firstPage;

        var remainingPages = await FetchRemainingPagesAsync<T>(schema, client,
            baseQuery,
            firstPage.Total,
            pageSize,
            queryOptions,
            ct);

        var allItems = firstPage.Items
            .Concat(remainingPages.SelectMany(p => p.Items))
            .ToList();

        return firstPage with { Items = allItems };
    }

    // ── private ──────────────────────────────────────────────────────────────

    private static async Task<ResponseSchema<T>[]> FetchRemainingPagesAsync<T>(string schema, ISquidexApiClient client,
        RequestQuery baseQuery,
        long total,
        int pageSize,
        QueryOptions? queryOptions,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pageCount = (int)Math.Ceiling(total / (double)pageSize);

        var pageTasks = Enumerable.Range(1, pageCount - 1)
            .Select(page => FetchPageAsync<T>(schema, client, PageQuery(baseQuery, page, pageSize), queryOptions, cts));

        return await Task.WhenAll(pageTasks);
    }

    private static async Task<ResponseSchema<T>> FetchPageAsync<T>(string schema, ISquidexApiClient client,
        RequestQuery query,
        QueryOptions? queryOptions,
        CancellationTokenSource cts)
    {
        try
        {
            return await client.QueryAsync<T>(schema, query, queryOptions, cts.Token);
        }
        catch
        {
            cts.Cancel();
            throw;
        }
    }

    private static RequestQuery PageQuery(RequestQuery source, int page, int pageSize) => new()
    {
        Take = pageSize,
        Skip = page * pageSize,
        Filter = source.Filter,
        Sort = source.Sort,
        FullText = source.FullText
    };
}
