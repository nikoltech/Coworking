using System.Data;

namespace Coworking.Application.Common.Interfaces.Transactions;

public interface ITransaction : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Returns the underlying database transaction object.
    /// </summary>
    IDbTransaction GetUnderlyingTransaction();

    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}