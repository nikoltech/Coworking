using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Coworking.Infrastructure.Repositories;

internal sealed class CoworkingRepository(AppDbContext context) : ICoworkingRepository
{
    public async Task<Domain.Entities.Coworking> FetchAsync(int deskId, CancellationToken ct) =>
        await context.Set<Desk>()
            .AsNoTracking()
            .Where(d => d.Id == deskId)
            .Select(d => d.Coworking)
            .SingleAsync(ct)
            .ConfigureAwait(false);

    public Task<List<Desk>> FetchDesksAsync(int coworkingId, CancellationToken ct)
    {
        return context.Set<Desk>()
            .AsNoTracking()
            .Where(d => d.CoworkingId == coworkingId)
            .ToListAsync(ct);
    }

    public async Task<Desk?> FetchDeskWithBookingsAsync(int deskId, DateTimeOffset targetDate, CancellationToken ct)
    {
        var utcTargetDate = targetDate.UtcDateTime;

        return await context.Set<Desk>()
            .AsNoTracking()
            .Include(d => d.Coworking)
            .Include(d => d.Bookings.Where(b => b.StartTime.Date == utcTargetDate.Date || b.EndTime.Date == utcTargetDate.Date))
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == deskId, ct);
    }
}
