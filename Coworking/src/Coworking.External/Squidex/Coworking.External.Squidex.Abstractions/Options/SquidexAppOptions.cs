using Coworking.External.Squidex.Abstractions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Coworking.External.Squidex.Abstractions.Options;

public sealed record SquidexAppOptions
{
    [Required] public string BaseUrl { get; init; } = string.Empty;
    [Required] public string AppName { get; init; } = string.Empty;

    public int MaxPageSize { get; init; } = 200;
    public string DefaultLocale { get; init; } = SquidexLocales.En;
    public string DefaultClient { get; init; } = "Default";

    /// <summary>
    /// Overrides locales fetched from Squidex app.
    /// appsettings always wins over Squidex app languages.
    /// </summary>
    public List<string> SupportedLocales { get; init; } = [];

    [Required]
    public Dictionary<string, SquidexClientCredentials> Clients { get; init; } = new();
}