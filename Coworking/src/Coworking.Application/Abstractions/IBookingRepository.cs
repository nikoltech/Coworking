using Coworking.Domain.Entities;

namespace Coworking.Application.Abstractions;

public interface IBookingRepository
{
    Task<bool> AnyOverlapAsync(
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task<Booking?> GetByIdAsync(int id, CancellationToken ct);
}
