using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Common.Interfaces.Transactions
{
    public interface IDbConflictDetector
    {
        /// <summary>
        /// Determines whether the specified exception represents a transient error condition.
        /// </summary>
        /// <remarks>Use this method to identify errors that are likely to be resolved by retrying the
        /// operation, such as network timeouts or temporary service unavailability. Non-transient exceptions typically
        /// require different handling.</remarks>
        /// <param name="ex">The exception to evaluate for transience. Cannot be null.</param>
        /// <returns>true if the exception is considered transient and the operation may be retried; otherwise, false.</returns>
        bool IsTransient(Exception ex);
    }
}
