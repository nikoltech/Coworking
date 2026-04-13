using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Repositories;

internal class BookingRepository(AppDbContext uow) : IBookingRepository
{
    public virtual async Task<bool> AnyOverlapAsync(Guid deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        return await uow.Set<Booking>()
            .AsNoTracking()
            .AnyAsync(BookingSpecifications.OverlappingWith(deskId, start, end), cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await uow.Set<Booking>().AddAsync(booking, cancellationToken);
    }
}
