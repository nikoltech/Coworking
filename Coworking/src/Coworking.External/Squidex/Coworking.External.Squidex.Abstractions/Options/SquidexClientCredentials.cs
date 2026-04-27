using System.ComponentModel.DataAnnotations;

namespace Coworking.External.Squidex.Abstractions.Options;

public sealed record SquidexClientCredentials
{
    [Required] public string ClientId { get; init; } = string.Empty;
    [Required] public string ClientSecret { get; init; } = string.Empty;
}