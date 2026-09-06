using Coworking.Domain.Constants;
using Coworking.API.Infrastructure.Validation;
using System.ComponentModel.DataAnnotations;

namespace Coworking.API.Models.Requests;

public record CreateBookingRequest
{
    [PositiveId(int.MaxValue)]
    public int DeskId { get; init; }

    [Required, EmailAddress, MaxLength(BookingLimits.UserEmailMaxLength)]
    public string UserEmail { get; init; } = default!;

    [Required, MaxLength(BookingLimits.UserNameMaxLength)]
    public string UserName { get; init; } = default!;

    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }

    public BookingMetadataRequest? Metadata { get; init; }
}
