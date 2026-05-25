using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Coworking.Infrastructure.Persistence.Configurations.Common;

/// <summary>
/// Normalizes DateTimeOffset to UTC before writing to PostgreSQL timestamptz.
/// Npgsql rejects any non-UTC offset; this converter makes all writes safe
/// regardless of the offset the application layer uses.
/// </summary>
public class DateTimeOffsetUtcConverter()
    : ValueConverter<DateTimeOffset, DateTimeOffset>(
        v => v.ToUniversalTime(),
        v => v);
