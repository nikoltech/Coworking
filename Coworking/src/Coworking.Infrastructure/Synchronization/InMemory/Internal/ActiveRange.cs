using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Infrastructure.Synchronization.InMemory;

internal sealed record ActiveRange(
    int DeskId,
    DateTimeOffset Start,
    DateTimeOffset End,
    SemaphoreSlim Semaphore,
    DateTimeOffset ExpiresAt);
