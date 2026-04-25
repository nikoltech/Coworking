using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Repository;

namespace Coworking.Application.Ports.Squidex.Schemas.City;

public interface ICityRepository : ISquidexRepository<CitySchema>
{
    Task<ContentDto<CitySchema>?> GetByTitleAsync(
        string title, CancellationToken ct = default);

    Task<ResponseSchema<CitySchema>> GetRegionCitiesAsync(
        CancellationToken ct = default);
}