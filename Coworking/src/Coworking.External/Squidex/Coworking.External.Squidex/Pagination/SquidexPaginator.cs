using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Pagination;

namespace Coworking.External.Squidex.Pagination;

/// <summary>
/// Fetches all pages of a Squidex query in bounded parallel batches.
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
        var maxParallel = client.AppOptions.Limits.MaxParallelRequests;

        var first = await FetchPageAsync<T>(schema, client, baseQuery, 0, pageSize, queryOptions, ct);
        var pages = new List<ResponseSchema<T>> { first };

        while (pages.Last().MayHaveMore(pageSize))
        {
            var fetched = pages.Count;

            var batch = await FetchBatchAsync<T>(schema, client, baseQuery,
                startPage: fetched,
                pageCount: NextBatchSize(first.Total, fetched, pageSize),
                pageSize, maxParallel, queryOptions, ct);

            pages.AddRange(batch);
        }

        return Combine(pages);
    }

    // private

    // no count to go by (-1) leaves nothing to predict, so take one page and look at it
    private static int NextBatchSize(long total, int fetched, int pageSize) =>
        total < 0 ? 1 : Math.Max(PagesFor(total, pageSize) - fetched, 1);

    private static int PagesFor(long total, int pageSize) =>
        (int)Math.Ceiling(total / (double)pageSize);

    private static async Task<ResponseSchema<T>[]> FetchBatchAsync<T>(string schema, ISquidexApiClient client,
        RequestQuery baseQuery,
        int startPage,
        int pageCount,
        int pageSize,
        int maxParallel,
        QueryOptions? queryOptions,
        CancellationToken ct)
    {
        var batch = new ResponseSchema<T>[pageCount];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, pageCount),
            new ParallelOptions { MaxDegreeOfParallelism = maxParallel, CancellationToken = ct },
            async (index, token) =>
                batch[index] = await FetchPageAsync<T>(
                    schema, client, baseQuery, startPage + index, pageSize, queryOptions, token));

        return batch;
    }

    private static Task<ResponseSchema<T>> FetchPageAsync<T>(string schema, ISquidexApiClient client,
        RequestQuery baseQuery,
        int page,
        int pageSize,
        QueryOptions? queryOptions,
        CancellationToken ct) =>
        client.QueryAsync<T>(schema, PageQuery(baseQuery, page, pageSize), queryOptions, ct);

    private static ResponseSchema<T> Combine<T>(List<ResponseSchema<T>> pages)
    {
        var items = pages.SelectMany(p => p.Items).ToList();

        return new ResponseSchema<T>(items.Count, items);   // counted, not the server's number
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
