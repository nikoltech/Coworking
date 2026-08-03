namespace Coworking.Application.Abstractions.Transactions
{
    public interface IDbConflictDetector
    {
        /// <summary>
        /// True when the exception is a retryable transaction conflict (serialization failure, deadlock).
        /// </summary>
        bool IsTransient(Exception ex);
    }
}
