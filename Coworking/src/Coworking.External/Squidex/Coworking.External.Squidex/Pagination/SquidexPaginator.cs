using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Options;
using Microsoft.Extensions.Options;

namespace Coworking.External.Squidex.Pagination;

/// <summary>
/// Fetches all pages of a Squidex query.
/// Page 1 is fetched first to determine total count.
/// Remaining pages are fetched in parallel.
/// </summary>
public sealed class SquidexPaginator(IOptions<SquidexOptions> options) : ISquidexPaginator
{
    private readonly int _pageSize = options.Value.MaxPageSize;

    public async Task<ResponseSchema<T>> FetchAllAsync<T>(
        string schema,
        ISquidexApiClient client,
        RequestQuery baseQuery,
        QueryOptions? queryOptions = null,
        CancellationToken ct = default)
    {
        baseQuery.Take = _pageSize;
        baseQuery.Skip = 0;

        var firstPage = await client.QueryAsync<T>(schema, baseQuery, queryOptions, ct);

        if (firstPage.Total <= _pageSize)
            return firstPage;

        var remainingPages = await FetchRemainingPagesAsync<T>(
            schema, client, baseQuery, firstPage.Total, queryOptions, ct);

        var allItems = firstPage.Items
            .Concat(remainingPages.SelectMany(p => p.Items))
            .ToList();

        return firstPage with { Items = allItems };
    }

    // ── private ──────────────────────────────────────────────────────────────

    private Task<ResponseSchema<T>[]> FetchRemainingPagesAsync<T>(
        string schema,
        ISquidexApiClient client,
        RequestQuery baseQuery,
        long total,
        QueryOptions? queryOptions,
        CancellationToken ct)
    {
        var pageCount = (int)Math.Ceiling(total / (double)_pageSize);

        var pageTasks = Enumerable.Range(1, pageCount - 1)
            .Select(page => client.QueryAsync<T>(
                schema,
                PageQuery(baseQuery, page),
                queryOptions,
                ct));

        return Task.WhenAll(pageTasks);
    }

    private RequestQuery PageQuery(RequestQuery source, int page) => new()
    {
        Take = _pageSize,
        Skip = page * _pageSize,
        Filter = source.Filter,
        Sort = source.Sort,
        FullText = source.FullText
    };
}