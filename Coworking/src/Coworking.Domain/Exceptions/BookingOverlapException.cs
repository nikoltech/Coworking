namespace Coworking.Domain.Exceptions;

public class BookingOverlapException : Exception
{
    public BookingOverlapException()
    {
    }

    public BookingOverlapException(string message)
        : base(message)
    {
    }

    public BookingOverlapException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
