namespace Coworking.Application.Abstractions;

public interface IBookingCacheService
{
    Task<bool> HasOverlapAsync(
        Guid deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct);
    Task AddAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task RemoveAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
}
