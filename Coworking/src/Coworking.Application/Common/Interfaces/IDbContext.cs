using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Common.Interfaces
{
    public interface IDbContext
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
