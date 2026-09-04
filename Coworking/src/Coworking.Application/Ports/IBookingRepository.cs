using Coworking.Domain.Entities;

namespace Coworking.Application.Ports;

public interface IBookingRepository
{
    Task<bool> AnyOverlapAsync(
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task<Booking?> FindByIdAsync(int id, CancellationToken ct);
}
