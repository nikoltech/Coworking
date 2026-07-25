using Coworking.Application.Abstractions;
using Coworking.Domain.Entities;
using Coworking.Domain.Specifications;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Coworking.Infrastructure.Repositories;

internal sealed class CoworkingRepository(AppDbContext context) : ICoworkingRepository
{
    public async Task<Desk?> FindDeskWithCoworkingAsync(
        int deskId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Desk>()
            .AsNoTracking()
            .Include(d => d.Coworking)
            .FirstOrDefaultAsync(d => d.Id == deskId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<List<Desk>> ListDesksAsync(int coworkingId,
        Expression<Func<Desk, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var query = context.Set<Desk>()
            .AsNoTracking()
            .Where(d => d.CoworkingId == coworkingId);

        if (predicate is { } filter)
            query = query.Where(filter);

        return await query.ToListAsync(ct);
    }

    public async Task<List<Domain.Entities.Coworking>> ListAsync(
        Expression<Func<Domain.Entities.Coworking, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var query = context.Set<Domain.Entities.Coworking>().AsNoTracking();

        if (predicate is { } filter)
            query = query.Where(filter);

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Desk?> FetchDeskWithBookingsAsync(int deskId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct = default)
    {
        var start = startUtc.ToUniversalTime();
        var end = endUtc.ToUniversalTime();

        var blockingBookings = context.Set<Booking>()
            .Where(b => b.DeskId == deskId && b.StartTime < end && b.EndTime > start)
            .Where(BookingSpecifications.IsBlocking());

        var result = await context.Set<Desk>()
            .AsNoTracking()
            .Where(d => d.Id == deskId)
            .GroupJoin(blockingBookings, d => d.Id, b => b.DeskId, (d, bookings) => new { Desk = d, Bookings = bookings })
            .FirstOrDefaultAsync(ct);

        if (result is null)
            return null;

        result.Desk.Bookings = result.Bookings.ToList();

        return result.Desk;
    }
}
