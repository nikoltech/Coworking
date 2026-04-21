using Coworking.Application.Abstractions.Transactions;
using Npgsql;

namespace Coworking.Infrastructure.Persistence.Transactions.Conflicts
{
    public class PostgresConflictDetector : IDbConflictDetector
    {
        public bool IsTransient(Exception ex)
        {
            if (ex.GetBaseException() is not PostgresException pgEx)
                return false;

            return pgEx.SqlState switch
            {
                "40001" => true, // Serialization failure
                "40P01" => true, // Deadlock detected
                _ => false
            };
        }
    }
}
