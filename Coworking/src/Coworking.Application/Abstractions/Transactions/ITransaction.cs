using System.Data;

namespace Coworking.Application.Abstractions.Transactions;

/// Note: For relational storages uses IDataContextTransaction, for non-relational storages uses ITransaction directly. 
/// This allows to avoid unnecessary wrapping of transactions for relational storages and provides a more flexible API for non-relational storages.
public interface ITransaction : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Returns the underlying database transaction object.
    /// </summary>
    IDbTransaction GetUnderlyingTransaction();

    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}