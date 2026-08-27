using System.Data;

namespace Coworking.Application.Abstractions.Transactions;

/// Note: For relational storages uses IDataContextTransaction, for non-relational storages uses ITransaction directly. 
/// This allows to avoid unnecessary wrapping of transactions for relational storages and provides a more flexible API for non-relational storages.
public interface ITransaction : IAsyncDisposable, IDisposable
{
    IDbTransaction GetUnderlyingTransaction();

    /// <summary>
    /// Commits the transaction. Takes no cancellation token on purpose: once COMMIT is sent
    /// the database decides the outcome, and abandoning the wait yields an unknown result
    /// rather than a rollback. Change your mind before calling this.
    /// </summary>
    Task CommitAsync();
    Task RollbackAsync(CancellationToken ct = default);
}