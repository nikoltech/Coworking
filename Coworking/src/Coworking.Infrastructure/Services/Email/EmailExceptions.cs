namespace Coworking.Infrastructure.Services.Email;

/// Everything an IEmailSender reports, so callers never see the SMTP client's own types.
public abstract class EmailDeliveryException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public class EmailTransientException(string message, Exception? innerException = null)
    : EmailDeliveryException(message, innerException);

/// The conversation never happened, so unlike a server rejection this is worth repeating at once.
public sealed class EmailConnectionException(string message, Exception? innerException = null)
    : EmailTransientException(message, innerException);

public sealed class EmailPermanentException(string message, Exception? innerException = null)
    : EmailDeliveryException(message, innerException);
