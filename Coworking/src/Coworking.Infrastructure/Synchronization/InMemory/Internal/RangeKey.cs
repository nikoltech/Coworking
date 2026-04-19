namespace Coworking.Infrastructure.Synchronization.InMemory.Internal;

// Struct for key (zero allocations)
internal readonly record struct RangeKey(int DeskId, DateTimeOffset Start, DateTimeOffset End);
