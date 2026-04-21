using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;

namespace Coworking.Application.Notifications.Email;

public interface IEmailNotificationService
{
    Task SendBookingCreatedAsync(BookingCreatedEmailModel model, CancellationToken ct);
    Task SendBookingCancelledAsync(BookingCancelledEmailModel model, CancellationToken ct);
}
