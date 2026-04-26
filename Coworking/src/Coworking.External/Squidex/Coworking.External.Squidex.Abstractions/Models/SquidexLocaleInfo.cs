namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Locale information from Squidex app languages API.
/// </summary>
public sealed record SquidexLocaleInfo(
    string Iso2Code,
    bool IsMaster,
    bool IsOptional);