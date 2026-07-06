using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Abstractions.Client;
using Microsoft.Extensions.Logging;

namespace Coworking.External.Squidex.Localization;

/// <summary>Resolves locales for a specific Squidex app.</summary>
public sealed class SquidexLocaleProvider
{
    private readonly SquidexAppOptions _appOptions;
    private readonly ILogger _logger;
    private readonly bool _hasExplicitDefault;
    private readonly bool _hasExplicitSupported;
    private IReadOnlyList<string>? _supportedLocales;
    private string? _defaultLocale;

    public SquidexLocaleProvider(SquidexAppOptions appOptions, ILogger<SquidexLocaleProvider> logger)
    {
        _appOptions = appOptions;
        _logger = logger;

        // Explicit = actually set in appsettings; both gate whether InitializeAsync needs Squidex at all.
        _hasExplicitDefault = !string.IsNullOrEmpty(appOptions.DefaultLocale);
        _hasExplicitSupported = appOptions.SupportedLocales.Count > 0;

        SeedFromConfig();
    }

    public string DefaultLocale => _defaultLocale ?? throw NotResolved();

    public IReadOnlyList<string> SupportedLocales => _supportedLocales ?? throw NotResolved();

    public async Task InitializeAsync(ISquidexApiClient client, CancellationToken ct = default)
    {
        var locales = await FetchAsync(client, ct);
        if (locales is null)
            return; // failure already invalidated + logged

        var master = locales.First(l => l.IsMaster); // Squidex guarantees exactly one
        EnsureMasterMatchesConfig(master.Iso2Code);

        _supportedLocales = _hasExplicitSupported ? _appOptions.SupportedLocales : locales.Select(l => l.Iso2Code).ToList();
        _defaultLocale = _hasExplicitDefault ? _appOptions.DefaultLocale : master.Iso2Code;

        _logger.LogInformation("Fetched locales from Squidex for app '{AppName}': {Locales}, default '{Default}'.",
            _appOptions.AppName, string.Join(",", _supportedLocales), _defaultLocale);

        Normalize();
    }

    private void SeedFromConfig()
    {
        // DefaultLocale alone is enough to serve a safe single-locale SupportedLocales until a fetch fills it in.
        if (_hasExplicitDefault)
        {
            _defaultLocale = _appOptions.DefaultLocale;
            _supportedLocales = _hasExplicitSupported ? _appOptions.SupportedLocales : [_appOptions.DefaultLocale];
        }
        else if (_hasExplicitSupported)
        {
            _supportedLocales = _appOptions.SupportedLocales;
        }

        Normalize();
    }

    private async Task<IReadOnlyList<SquidexLocaleInfo>?> FetchAsync(ISquidexApiClient client, CancellationToken ct)
    {
        IReadOnlyList<SquidexLocaleInfo> locales;
        try
        {
            locales = await client.GetAppLocalesAsync(ct);
        }
        catch (Exception ex)
        {
            InvalidateAndLog(ex);
            return null;
        }

        if (locales.Count == 0)
        {
            InvalidateAndLog(null);
            return null;
        }

        return locales;
    }

    /// <remarks>Skips if DefaultLocale isn't explicitly configured.</remarks>
    private void EnsureMasterMatchesConfig(string masterIso2Code)
    {
        if (!_hasExplicitDefault || string.Equals(masterIso2Code, _appOptions.DefaultLocale, StringComparison.Ordinal))
            return;

        Invalidate();

        throw new InvalidOperationException(
            $"Squidex app '{_appOptions.AppName}': configured DefaultLocale '{_appOptions.DefaultLocale}' " +
            $"does not match Squidex's actual master locale '{masterIso2Code}'.");
    }

    private void InvalidateAndLog(Exception? ex)
    {
        Invalidate();

        _logger.LogCritical(ex,
            "Squidex app '{AppName}': locale fetch failed or returned none — " +
            "content queries for this app will throw until this is fixed.",
            _appOptions.AppName);
    }

    private void Invalidate()
    {
        _defaultLocale = null;
        _supportedLocales = null;
    }

    private void Normalize()
    {
        if (_defaultLocale is null || _supportedLocales is null)
            return;

        _supportedLocales = _supportedLocales.Append(_defaultLocale).Distinct().ToList();
    }

    private InvalidOperationException NotResolved() => new(
        $"Squidex locales for app '{_appOptions.AppName}' are not resolved — " +
        "set DefaultLocale explicitly in configuration, or ensure Squidex is reachable at startup.");
}
