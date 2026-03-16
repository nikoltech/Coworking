using Coworking.Application.Common.Interfaces.Transactions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Coworking.Infrastructure.Persistence.Transactions;

public class EfTransactionWrapper(IDbContextTransaction efTransaction) : ITransaction
{
    public IDbTransaction GetUnderlyingTransaction() => efTransaction.GetDbTransaction();

    public Task CommitAsync(CancellationToken ct = default) => efTransaction.CommitAsync(ct);

    public Task RollbackAsync(CancellationToken ct = default) => efTransaction.RollbackAsync(ct);

    public void Dispose() => efTransaction.Dispose();

    public ValueTask DisposeAsync() => efTransaction.DisposeAsync();
}
