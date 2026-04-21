namespace Coworking.Infrastructure.Services.Email.Templates.Models;

internal record BookingCancelledTemplateModel(
    string To,
    string UserName,
    string DeskName,
    string CoworkingName,
    string FormattedStart,
    string FormattedEnd,
    string TimeZoneId,
    string? CancellationReason);
