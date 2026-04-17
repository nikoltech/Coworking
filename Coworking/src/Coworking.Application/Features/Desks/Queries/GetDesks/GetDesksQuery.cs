using Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Features.Desks.Queries.GetDesks;

public record GetDesksQuery(int coworkingId) : IRequest<IReadOnlyList<DeskDto>>;
