using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Repositories;

internal class BookingRepository(AppDbContext context) : IBookingRepository
{
    public virtual async Task<bool> AnyOverlapAsync(int deskId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        return await context.Set<Booking>()
            .AsNoTracking()
            .AnyAsync(BookingSpecifications.OverlappingWith(deskId, start.ToUniversalTime(), end.ToUniversalTime()), cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await context.Set<Booking>().AddAsync(booking, cancellationToken);
    }
}
