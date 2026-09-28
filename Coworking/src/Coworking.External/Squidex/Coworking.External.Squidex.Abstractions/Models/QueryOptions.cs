namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Per-request Squidex query options.
/// Controls X-Languages, X-Unpublished, X-NoSlowTotal and X-Flatten headers.
/// </summary>
public sealed record QueryOptions
{
    public static readonly QueryOptions Default = new();

    /// <summary>
    /// Locales to return via X-Languages header. Null or empty returns every locale.
    /// Narrowing is a read optimisation: writing a narrowed item back drops the locales left out.
    /// </summary>
    public List<string>? Languages { get; init; }

    /// <summary>Include draft and unpublished content via X-Unpublished header.</summary>
    public bool IncludeUnpublished { get; init; }

    /// <summary>
    /// Skip total count for faster queries via X-NoSlowTotal header.
    /// Use when total is not needed (e.g. ExistsAsync).
    /// </summary>
    public bool NoSlowTotal { get; init; }

    /// <summary>
    /// Flatten localized and invariant fields to scalar values via X-Flatten header.
    /// Schema DTO must use plain C# types — NOT IvField or LocalizedField.
    /// Use only for special-case queries where you control the DTO shape.
    /// </summary>
    public bool Flatten { get; init; }

}