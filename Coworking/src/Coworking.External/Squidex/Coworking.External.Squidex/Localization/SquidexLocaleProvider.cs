using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Options;
using Microsoft.Extensions.Options;

namespace Coworking.External.Squidex.Localization;

/// <summary>
/// Resolves supported locales for X-Languages header.
/// Priority: appsettings.SupportedLocales > Squidex app languages > DefaultLocale fallback.
///
/// Call InitializeAsync once on application startup before serving requests.
/// </summary>
public sealed class SquidexLocaleProvider(IOptions<SquidexOptions> options)
{
    private readonly SquidexOptions _options = options.Value;
    private IReadOnlyList<string>? _resolved;

    public string DefaultLocale => _options.DefaultLocale;

    public IReadOnlyList<string> SupportedLocales =>
        _resolved ?? (_options.SupportedLocales.Count > 0
            ? _options.SupportedLocales
            : [_options.DefaultLocale]);

    /// <summary>
    /// Fetches locales from Squidex app if not set in appsettings.
    /// Safe to call multiple times — resolves only once.
    /// </summary>
    public async Task InitializeAsync(ISquidexApiClient client, CancellationToken ct = default)
    {
        if (_resolved is not null)
            return;

        if (_options.SupportedLocales.Count > 0)
        {
            _resolved = _options.SupportedLocales;
            return;
        }

        try
        {
            var locales = await client.GetAppLocalesAsync(ct);
            _resolved = locales.Count > 0 ? locales : [_options.DefaultLocale];
        }
        catch
        {
            // Squidex unreachable on startup — fall back to default
            _resolved = [_options.DefaultLocale];
        }
    }
}