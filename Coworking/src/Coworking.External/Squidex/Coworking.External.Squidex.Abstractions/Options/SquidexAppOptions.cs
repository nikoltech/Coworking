using System.ComponentModel.DataAnnotations;

namespace Coworking.External.Squidex.Abstractions.Options;

public sealed record SquidexAppOptions
{
    [Required] public string BaseUrl { get; init; } = string.Empty;
    [Required] public string AppName { get; init; } = string.Empty;

    public int MaxPageSize { get; init; } = 200;
    public string DefaultClient { get; init; } = "Default";

    [Required]
    public Dictionary<string, SquidexClientCredentials> Clients { get; init; } = new();

    public SquidexRetryOptions Retry { get; init; } = new();

    public SquidexLimitsOptions Limits { get; init; } = new();
}