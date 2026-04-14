using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Common.Synchronization;

public interface IBookingOverlapGate
{
    Task<IAsyncDisposable> AcquireAsync(int deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task<IAsyncDisposable> AcquireAsync(TimeSpan? ttl, int deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
}
