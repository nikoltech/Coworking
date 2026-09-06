using Coworking.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Coworking.API.Models.Requests;

public record BookingMetadataRequest
{
    [MaxLength(BookingLimits.UserTimeZoneMaxLength)]
    public string? UserTimeZoneId { get; init; }
}
