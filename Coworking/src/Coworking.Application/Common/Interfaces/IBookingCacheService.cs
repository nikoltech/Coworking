using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Common.Interfaces;

public interface IBookingCacheService
{
    Task<bool> HasOverlapAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task AddAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task RemoveAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
}
