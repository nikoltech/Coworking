using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Transactions;
using Coworking.Application.Common.Enums;
using Coworking.Infrastructure.Persistence.Extensions;
using Coworking.Infrastructure.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Infrastructure.Persistence.Contexts;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyGlobalConfiguration();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var efTransaction = await Database.BeginTransactionAsync(ct);
        return new EfTransactionWrapper(efTransaction);
    }

    public async Task<ITransaction> BeginTransactionAsync(TransactionIsolationLevel isolationLevel, CancellationToken ct = default)
    {
        var efTransaction = await Database.BeginTransactionAsync(isolationLevel.ToSqlType(), ct);
        return new EfTransactionWrapper(efTransaction);
    }
}
