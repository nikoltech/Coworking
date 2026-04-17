using AutoMapper;
using Coworking.Application.Common.Interfaces;
using Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;
using Coworking.Domain.Entities;
using MediatR;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Features.Desks.Queries.GetDesks;

public class GetDesksQueryHandler(ICoworkingRepository repository, IMapper mapper) 
    : IRequestHandler<GetDesksQuery, IReadOnlyList<DeskDto>>
{
    public async Task<IReadOnlyList<DeskDto>> Handle(GetDesksQuery request, CancellationToken ct)
    {
        var entities = await repository.FetchDesksAsync(request.coworkingId, ct);
        return mapper.Map<IReadOnlyList<DeskDto>>(entities);
    }
}