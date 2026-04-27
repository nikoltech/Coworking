using System.ComponentModel.DataAnnotations;

namespace Coworking.External.Squidex.Abstractions.Options;

public sealed record SquidexGlobalOptions
{
    public const string SectionName = "Squidex";

    [Required]
    public Dictionary<string, SquidexAppOptions> Apps { get; init; } = new();
}