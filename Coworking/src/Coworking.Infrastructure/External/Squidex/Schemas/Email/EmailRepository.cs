using Coworking.Application.Ports.Squidex.Schemas.Email;
using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Pagination;
using Coworking.External.Squidex.Repository;
using Microsoft.Extensions.Caching.Memory;

namespace Coworking.Infrastructure.External.Squidex.Schemas.Email;

public sealed class EmailRepository(
    SquidexApiClient client,
    SquidexPaginator paginator,
    IMemoryCache cache)
    : SquidexRepository<EmailSchema>(client, paginator, "emails"), IEmailRepository
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    public async Task<string?> GetEmailsByNameAsync(
        string name, CancellationToken ct = default)
    {
        var cacheKey = $"squidex:emails:{name}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;

            var result = await QueryAsync(
                RequestQuery.Create()
                    .WithTake(1)
                    .WithFilter(SquidexFilter.Eq(EmailPaths.Name, name)),
                ct: ct);

            return result.Items.FirstOrDefault()?.Data.Value?.Value;
        });
    }
}