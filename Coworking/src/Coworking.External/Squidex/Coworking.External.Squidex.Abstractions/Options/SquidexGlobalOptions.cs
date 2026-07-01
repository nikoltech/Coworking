using System.ComponentModel.DataAnnotations;

namespace Coworking.External.Squidex.Abstractions.Options;

public sealed record SquidexGlobalOptions
{
    public const string SectionName = "Squidex";

    /// <summary>
    /// Optional. The app served by a keyless <c>ISquidexContext</c> when several apps
    /// are configured. Ignored when only one app is configured (that one is keyless anyway).
    /// </summary>
    public string? DefaultApp { get; init; }

    [Required]
    public Dictionary<string, SquidexAppOptions> Apps { get; init; } = new();
}