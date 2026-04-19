using AutoMapper;
using Coworking.Application.Common.Interfaces;
using Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;
using MediatR;

namespace Coworking.Application.Features.Desks.Queries.GetDesks;

public record GetDesksQuery(int coworkingId)
    : IRequest<IReadOnlyList<DeskDto>>;

public class GetDesksQueryHandler(ICoworkingRepository repository, IMapper mapper) : IRequestHandler<GetDesksQuery, IReadOnlyList<DeskDto>>
{
    public async Task<IReadOnlyList<DeskDto>> Handle(GetDesksQuery request, CancellationToken ct)
    {
        var entities = await repository.ListDesksAsync(request.coworkingId, default, ct);
        return mapper.Map<IReadOnlyList<DeskDto>>(entities);
    }
}