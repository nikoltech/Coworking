namespace Coworking.Application.Common.Exceptions;

/// The retries ran out on a database conflict. The same request can still succeed.
public sealed class TransactionConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
