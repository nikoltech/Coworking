namespace Coworking.Domain.Exceptions;

/// <summary>
/// A broken invariant: the caller or the stored data is wrong, not the client. Deliberately left
/// out of ExceptionStatusMap, so it surfaces as 500 and gets logged as an error.
/// </summary>
public sealed class DomainInvariantException(string message, Exception? innerException = null)
    : Exception(message, innerException);
