using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Features.Coworkings.Queries.GetCoworkings;

public record GetCoworkingsQuery : IRequest<IEnumerable<CoworkingSummaryDto>>;
