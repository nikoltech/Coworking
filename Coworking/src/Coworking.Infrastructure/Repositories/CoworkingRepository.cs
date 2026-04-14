using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Repositories;

internal sealed class CoworkingRepository(AppDbContext db) : ICoworkingRepository
{
    public async Task<Domain.Entities.Coworking> GetByDeskIdAsync(int deskId, CancellationToken ct) =>
        await db.Set<Desk>()
            .AsNoTracking()
            .Where(d => d.Id == deskId)
            .Select(d => d.Coworking)
            .SingleAsync(ct)
            .ConfigureAwait(false);
}
