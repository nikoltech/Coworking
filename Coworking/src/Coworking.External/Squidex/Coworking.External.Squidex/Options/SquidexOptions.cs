using Coworking.External.Squidex.Abstractions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Coworking.External.Squidex.Options;

public sealed record SquidexOptions
{
    public const string SectionName = "Squidex";

    [Required] public string BaseUrl { get; init; } = string.Empty;
    [Required] public string AppName { get; init; } = string.Empty;

    public int MaxPageSize { get; init; } = 200;
    public string DefaultLocale { get; init; } = SquidexLocales.UkUA;

    /// <summary>
    /// Locales to request via X-Languages header.
    /// If empty — fetched from Squidex app on startup.
    /// appsettings always overrides Squidex app languages.
    /// </summary>
    public List<string> SupportedLocales { get; init; } = [];

    [Required]
    public Dictionary<string, SquidexClientCredentials> Clients { get; init; } = new();
}

public sealed record SquidexClientCredentials
{
    [Required] public string ClientId { get; init; } = string.Empty;
    [Required] public string ClientSecret { get; init; } = string.Empty;
}