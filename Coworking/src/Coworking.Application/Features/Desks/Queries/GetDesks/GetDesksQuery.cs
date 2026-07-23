using AutoMapper;
using AutoMapper.QueryableExtensions;
using Coworking.Application.Abstractions;
using Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;
using Coworking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace Coworking.Application.Features.Desks.Queries.GetDesks;

public record GetDesksQuery(int coworkingId) : IRequest<IReadOnlyList<DeskDto>>;

public class GetDesksQueryHandler(IAppDbContext context, IMapper mapper) : IRequestHandler<GetDesksQuery, IReadOnlyList<DeskDto>>
{
    public async Task<IReadOnlyList<DeskDto>> Handle(GetDesksQuery request, CancellationToken ct)
    {
        return await context.Set<Desk>()
            .Where(x => x.CoworkingId == request.coworkingId)
            .OrderBy(x => x.Name)
            .ProjectTo<DeskDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }
}