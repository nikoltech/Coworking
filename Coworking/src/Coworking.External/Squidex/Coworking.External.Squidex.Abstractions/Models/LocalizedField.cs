namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Squidex localized field. Key = locale (e.g. "uk-UA", "en").
/// Only locales requested via X-Languages are returned.
/// </summary>
public sealed class LocalizedField<T> : Dictionary<string, T>
{
    public T? Get(string locale) =>
        TryGetValue(locale, out var value) ? value : default;

    /// <summary>Returns value for locale, falls back to defaultLocale.</summary>
    public T? GetLocalized(string locale, string defaultLocale) =>
        Get(locale) ?? Get(defaultLocale);
}