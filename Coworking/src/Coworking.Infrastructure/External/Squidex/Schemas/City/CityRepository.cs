using Coworking.Application.Ports.Squidex.Schemas.City;
using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Pagination;
using Coworking.External.Squidex.Repository;

namespace Coworking.Infrastructure.External.Squidex.Schemas.City;

public sealed class CityRepository(SquidexApiClient client, SquidexPaginator paginator)
    : SquidexRepository<CitySchema>(client, paginator, "city"), ICityRepository
{
    public async Task<ContentDto<CitySchema>?> GetByTitleAsync(
        string title, CancellationToken ct = default)
    {
        var result = await QueryAsync(
            RequestQuery.Create()
                .WithTake(1)
                .WithFilter(SquidexFilter.Eq(CityPaths.Title, title)),
            ct: ct);

        return result.Items.FirstOrDefault();
    }

    public Task<ResponseSchema<CitySchema>> GetRegionCitiesAsync(
        CancellationToken ct = default) =>
        GetAllAsync(
            RequestQuery.Create()
                .WithFilter(SquidexFilter.Eq(CityPaths.IsRegionCity, true))
                .WithSort([SortOption.Asc(CityPaths.SOrder)]),
            ct: ct);
}