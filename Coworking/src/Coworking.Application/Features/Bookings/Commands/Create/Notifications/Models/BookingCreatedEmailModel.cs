namespace Coworking.Application.Features.Bookings.Commands.Create.Notifications.Models;

public sealed record BookingCreatedEmailModel(
    string To,
    string UserName,
    string DeskName,
    string CoworkingName,
    string FormattedStart,
    string FormattedEnd,
    string TimeZoneId);
