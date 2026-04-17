using Coworking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Common.Interfaces;

public interface ICoworkingRepository
{
    Task<Domain.Entities.Coworking> FetchAsync(int deskId, CancellationToken ct);

    Task<List<Desk>> FetchDesksAsync(int coworkingId, CancellationToken ct);
    Task<Desk?> FetchDeskWithBookingsAsync(int deskId, DateTimeOffset targetDate, CancellationToken ct);
}
