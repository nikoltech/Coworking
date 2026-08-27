using Coworking.Application.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Coworking.Infrastructure.Persistence.Transactions;

public class EfTransactionWrapper(IDbContextTransaction efTransaction) : ITransaction
{
    public IDbTransaction GetUnderlyingTransaction() => efTransaction.GetDbTransaction();

    public Task CommitAsync() => efTransaction.CommitAsync(CancellationToken.None);

    public Task RollbackAsync(CancellationToken ct = default) => efTransaction.RollbackAsync(ct);

    public void Dispose() => efTransaction.Dispose();

    public ValueTask DisposeAsync() => efTransaction.DisposeAsync();
}
