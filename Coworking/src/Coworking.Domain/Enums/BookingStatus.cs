namespace Coworking.Domain.Enums
{
    public enum BookingStatus
    {
        Unknown,

        Created,

        PendingPayment,

        PendingConfirmation,

        Confirmed,

        Cancelling,

        Cancelled,

        Expired
    }
}
