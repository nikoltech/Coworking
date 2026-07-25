using Coworking.Application.Abstractions;
using Coworking.Domain.Entities;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Repositories;

internal class BookingRepository(AppDbContext context) : IBookingRepository
{
    public virtual async Task<bool> AnyOverlapAsync(
        int deskId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        return await context.Set<Booking>()
            .AsNoTracking()
            .Where(b => b.EndTime > DateTimeOffset.UtcNow)
            .Where(BookingSpecifications.IsBlocking())
            .AnyAsync(BookingSpecifications.OverlappingWith(deskId, start.ToUniversalTime(), end.ToUniversalTime()), cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        await context.Set<Booking>().AddAsync(booking, cancellationToken);
    }

    public Task<Booking?> FindByIdAsync(int id, CancellationToken ct)
    {
        return context.Set<Booking>()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }
}
