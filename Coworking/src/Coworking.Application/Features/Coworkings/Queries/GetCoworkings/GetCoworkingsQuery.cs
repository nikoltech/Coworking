using AutoMapper;
using AutoMapper.QueryableExtensions;
using Coworking.Application.Common.Interfaces;
using Coworking.Application.Features.Coworkings.Queries.GetCoworkings.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Features.Coworkings.Queries.GetCoworkings;

public record GetCoworkingsQuery : IRequest<IReadOnlyList<CoworkingDto>>;

internal sealed class GetCoworkingsQueryHandler(IAppDbContext context, IMapper mapper) : IRequestHandler<GetCoworkingsQuery, IReadOnlyList<CoworkingDto>>
{
    public async Task<IReadOnlyList<CoworkingDto>> Handle(GetCoworkingsQuery request, CancellationToken ct)
    {
        return await context.Set<Domain.Entities.Coworking>()
            .OrderBy(c => c.TimeZoneId)
            .ThenBy(c => c.Name)
            .ProjectTo<CoworkingDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}