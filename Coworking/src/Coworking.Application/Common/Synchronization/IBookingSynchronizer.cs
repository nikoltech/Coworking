using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Common.Synchronization;

public interface IBookingSynchronizer
{
    Task<IAsyncDisposable> AcquireAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task<IAsyncDisposable> AcquireAsync(TimeSpan? ttl, Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
}
