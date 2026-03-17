using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Repositories;

internal class BookingRepository(AppDbContext uow) : IBookingRepository
{
    public virtual async Task<bool> AnyOverlapAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        return await uow.Set<Booking>()
            .AsNoTracking()
            .AnyAsync(b => b.DeskId == deskId && b.StartTime < end && b.EndTime > start, cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await uow.Set<Booking>().AddAsync(booking, cancellationToken);
    }
}
