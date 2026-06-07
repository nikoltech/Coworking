using Coworking.Application.Abstractions;
using Coworking.Domain.Entities;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Coworking.Infrastructure.Repositories;

internal sealed class CoworkingRepository(AppDbContext context) : ICoworkingRepository
{
    public async Task<Desk> GetDeskWithCoworkingAsync(
        int deskId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Desk>()
            .Where(d => d.Id == deskId)
            .Include(d => d.Coworking)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<List<Desk>> ListDesksAsync(int coworkingId,
        Expression<Func<Desk, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var query = context.Set<Desk>()
            .Where(d => d.CoworkingId == coworkingId);

        if (predicate is { } filter)
            query = query.Where(filter);

        return await query.ToListAsync(ct);
    }

    public async Task<List<Domain.Entities.Coworking>> ListAsync(
        Expression<Func<Domain.Entities.Coworking, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var query = context.Set<Domain.Entities.Coworking>().AsQueryable();

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

        return await context.Set<Desk>()
            .AsNoTracking()
            .Include(d => d.Bookings.Where(b =>
                b.StartTime < end &&
                b.EndTime > start))
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == deskId, ct);
    }
}
