
namespace Coworking.Application.Features.Bookings.Commands.Cancel.Notifications.Models;

public sealed record BookingCancelledEmailModel(
    string To,
    string UserName,
    string DeskName,
    string CoworkingName,
    string FormattedStart,
    string FormattedEnd,
    string TimeZoneId,
    string? CancellationReason);
