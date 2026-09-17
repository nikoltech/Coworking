namespace Coworking.Application.Common.Exceptions;

/// The request waited too long for a shared resource. The same request can still succeed later.
public sealed class ServiceBusyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
