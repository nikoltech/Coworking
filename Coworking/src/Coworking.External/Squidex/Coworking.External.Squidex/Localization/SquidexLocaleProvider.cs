// External.Squidex/Localization/SquidexLocaleProvider.cs
using Coworking.External.Squidex.Abstractions.Localization;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Options;
using Microsoft.Extensions.Options;

namespace Coworking.External.Squidex.Localization;

/// <summary>
/// Resolves supported locales and default locale for X-Languages header.
///
/// Priority for DefaultLocale:
///   1. appsettings.DefaultLocale (explicit)
///   2. Squidex app language marked as IsMaster
///   3. SquidexLocales.UkUA (fallback constant)
///
/// Priority for SupportedLocales:
///   1. appsettings.SupportedLocales (explicit)
///   2. All locales from Squidex app
///   3. [DefaultLocale] (fallback)
///
/// Call InitializeAsync once on startup before serving requests.
/// </summary>
public sealed class SquidexLocaleProvider
{
    private readonly SquidexOptions _options;
    private IReadOnlyList<string>? _supportedLocales;
    private string? _defaultLocale;

    public SquidexLocaleProvider(IOptions<SquidexOptions> options) =>
        _options = options.Value;

    public string DefaultLocale =>
        _defaultLocale ?? _options.DefaultLocale;

    public IReadOnlyList<string> SupportedLocales =>
        _supportedLocales ?? (_options.SupportedLocales.Count > 0
            ? _options.SupportedLocales
            : [DefaultLocale]);

    /// <summary>
    /// Fetches locales from Squidex app if not configured in appsettings.
    /// Safe to call multiple times — resolves only once.
    /// </summary>
    public async Task InitializeAsync(
        ISquidexApiClient client, CancellationToken ct = default)
    {
        // Already initialized — skip
        if (_supportedLocales is not null)
            return;

        // appsettings wins for SupportedLocales
        if (_options.SupportedLocales.Count > 0)
        {
            _supportedLocales = _options.SupportedLocales;
            // DefaultLocale stays from appsettings — no need to fetch
            return;
        }

        try
        {
            var locales = await client.GetAppLocalesAsync(ct);

            if (locales.Count == 0)
            {
                _supportedLocales = [_options.DefaultLocale];
                return;
            }

            _supportedLocales = locales
                .Select(l => l.Iso2Code)
                .ToList();

            // Set DefaultLocale from IsMaster only if not explicitly set in appsettings
            if (_options.DefaultLocale == SquidexLocales.En)
            {
                var master = locales.FirstOrDefault(l => l.IsMaster);
                if (master is not null)
                    _defaultLocale = master.Iso2Code;
            }
        }
        catch
        {
            // Squidex unreachable on startup — graceful fallback
            _supportedLocales = [_options.DefaultLocale];
        }
    }
}