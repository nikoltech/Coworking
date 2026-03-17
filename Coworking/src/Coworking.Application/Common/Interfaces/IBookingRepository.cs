using Coworking.Domain.Entities;

namespace Coworking.Application.Common.Interfaces;

public interface IBookingRepository
{
    Task<bool> AnyOverlapAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
}
