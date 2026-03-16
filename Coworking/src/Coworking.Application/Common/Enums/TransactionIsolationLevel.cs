namespace Coworking.Application.Common.Enums;

/// <summary>
/// Transaction isolation levels (ACID).
/// Define how changes made by one transaction are visible to others.
/// </summary>
public enum TransactionIsolationLevel
{
    /// <summary>
    /// (Default in most DBs). Reads only committed data.
    /// Prevents Dirty Read. Non-repeatable Read is possible.
    /// </summary>
    ReadCommitted,

    /// <summary>
    /// Allows reading uncommitted data. "Dirty Read" is possible.
    /// The fastest but most dangerous level.
    /// </summary>
    ReadUncommitted,

    /// <summary>
    /// Guarantees that data read once will not change until the end of the transaction.
    /// Prevents Non-repeatable Read. Phantom Read is possible.
    /// </summary>
    RepeatableRead,

    /// <summary>
    /// Optimistic isolation (MVCC). The transaction sees a snapshot of the data as it existed at the start.
    /// Prevents Dirty, Non-repeatable, and Phantom Reads without locking, but requires handling update conflicts.
    /// </summary>
    Snapshot,

    /// <summary>
    /// The highest level. Full isolation via Range Locks.
    /// Completely eliminates anomalies but significantly reduces concurrency.
    /// </summary>
    Serializable
}
