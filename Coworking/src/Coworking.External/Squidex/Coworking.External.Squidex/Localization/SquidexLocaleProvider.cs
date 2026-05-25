using Coworking.External.Squidex.Abstractions.Localization;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Abstractions.Repository;

namespace Coworking.External.Squidex.Localization;

/// <summary>
/// Resolves locales for a specific Squidex app.
/// Priority: appsettings.SupportedLocales → Squidex app languages (IsMaster → DefaultLocale) → fallback.
/// </summary>
public sealed class SquidexLocaleProvider
{
    private readonly SquidexAppOptions _appOptions;
    private IReadOnlyList<string>? _supportedLocales;
    private string? _defaultLocale;

    public SquidexLocaleProvider(SquidexAppOptions appOptions) =>
        _appOptions = appOptions;

    public string DefaultLocale =>
        _defaultLocale ?? _appOptions.DefaultLocale;

    public IReadOnlyList<string> SupportedLocales =>
        _supportedLocales ?? (_appOptions.SupportedLocales.Count > 0
            ? _appOptions.SupportedLocales
            : [DefaultLocale]);

    public async Task InitializeAsync(ISquidexApiClient client, CancellationToken ct = default)
    {
        if (_supportedLocales is not null)
            return;

        if (_appOptions.SupportedLocales.Count > 0)
        {
            _supportedLocales = _appOptions.SupportedLocales;
            return;
        }

        try
        {
            var locales = await client.GetAppLocalesAsync(ct);

            if (locales.Count == 0)
            {
                _supportedLocales = [_appOptions.DefaultLocale];
                return;
            }

            _supportedLocales = locales.Select(l => l.Iso2Code).ToList();

            // Override DefaultLocale with IsMaster only if not explicitly set in appsettings
            if (string.IsNullOrEmpty(_appOptions.DefaultLocale) || _appOptions.DefaultLocale == SquidexLocales.Default)
            {
                var master = locales.FirstOrDefault(l => l.IsMaster);
                if (master is not null)
                    _defaultLocale = master.Iso2Code;
            }
        }
        catch
        {
            _supportedLocales = [_appOptions.DefaultLocale];
        }
    }
}