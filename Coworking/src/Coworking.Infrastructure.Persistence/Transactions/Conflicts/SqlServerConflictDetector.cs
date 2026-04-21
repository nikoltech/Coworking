using Coworking.Application.Abstractions.Transactions;

namespace Coworking.Infrastructure.Persistence.Transactions.Conflicts
{
    public class SqlServerConflictDetector : IDbConflictDetector
    {
        public bool IsTransient(Exception ex)
        {
            throw new NotImplementedException(
                "IsTransient is not implemented. Intended to detect SQL Server transient errors: deadlock (1205), snapshot/serializable conflict (3960), lock timeout (1222)."
            );

            //if (ex.GetBaseException() is not SqlException sqlEx)
            //    return false;
            //
            //return sqlEx.Number switch
            //{
            //    1205 => true, // Deadlock victim
            //    3960 => true, // Snapshot/Serializable conflict
            //    1222 => true, // Lock timeout
            //    _ => false
            //};
        }
    }
}
