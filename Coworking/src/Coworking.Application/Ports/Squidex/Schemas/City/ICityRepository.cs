using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Set;

namespace Coworking.Application.Ports.Squidex.Schemas.City;

public interface ICityRepository : ISquidexSet<CitySchema>
{
    Task<ContentDto<CitySchema>?> GetByTitleAsync(
        string title, CancellationToken ct = default);

    Task<ResponseSchema<CitySchema>> GetRegionCitiesAsync(CancellationToken ct = default);
}