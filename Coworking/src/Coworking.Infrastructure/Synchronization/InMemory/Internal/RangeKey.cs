using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Infrastructure.Synchronization.InMemory.Internal;

// Struct for key (zero allocations)
internal readonly record struct RangeKey(int DeskId, DateTimeOffset Start, DateTimeOffset End);
