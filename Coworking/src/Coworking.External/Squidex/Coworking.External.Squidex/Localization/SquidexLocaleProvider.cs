using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Abstractions.Client;
using Microsoft.Extensions.Logging;

namespace Coworking.External.Squidex.Localization;

/// <summary>Resolves locales for a specific Squidex app.</summary>
public sealed class SquidexLocaleProvider
{
    private sealed record LocaleState(string Default, IReadOnlyList<string> Supported);

    private readonly SquidexAppOptions _appOptions;
    private readonly ILogger _logger;
    private readonly bool _hasExplicitDefault;
    private readonly bool _hasExplicitSupported;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private volatile LocaleState? _state;

    public SquidexLocaleProvider(SquidexAppOptions appOptions, ILogger<SquidexLocaleProvider> logger)
    {
        _appOptions = appOptions;
        _logger = logger;

        _hasExplicitDefault = !string.IsNullOrEmpty(appOptions.DefaultLocale);
        _hasExplicitSupported = appOptions.SupportedLocales.Count > 0;

        _state = SeedFromConfig();
    }

    public string DefaultLocale => Resolved().Default;

    public IReadOnlyList<string> SupportedLocales => Resolved().Supported;

    /// <summary>
    /// Replaces whatever configuration did not pin with the app's own languages.
    /// Throws if Squidex cannot answer, leaving the previous state untouched.
    /// </summary>
    public async Task SyncAsync(ISquidexApiClient client, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var locales = await FetchAsync(client, ct);
            var master = Master(locales);

            EnsureMasterMatchesConfig(master);

            var state = Compose(
                _hasExplicitDefault ? _appOptions.DefaultLocale : master,
                _hasExplicitSupported ? _appOptions.SupportedLocales : [.. locales.Select(l => l.Iso2Code)]);

            _state = state;

            _logger.LogInformation("Squidex app '{AppName}': locales {Locales}, default '{Default}'.",
                _appOptions.AppName, string.Join(",", state.Supported), state.Default);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Checks configuration against the app's own languages. Never changes state.</summary>
    public async Task ValidateAsync(ISquidexApiClient client, CancellationToken ct = default) =>
        EnsureMasterMatchesConfig(Master(await FetchAsync(client, ct)));

    private LocaleState? SeedFromConfig() =>
        _hasExplicitDefault
            ? new LocaleState(
                _appOptions.DefaultLocale,
                _hasExplicitSupported ? _appOptions.SupportedLocales : [_appOptions.DefaultLocale])
            : null;

    private async Task<IReadOnlyList<SquidexLocaleInfo>> FetchAsync(ISquidexApiClient client, CancellationToken ct)
    {
        var locales = await client.GetAppLocalesAsync(ct);

        return locales.Count > 0
            ? locales
            : throw new InvalidOperationException(
                $"Squidex app '{_appOptions.AppName}' returned no languages.");
    }

    private static string Master(IReadOnlyList<SquidexLocaleInfo> locales) =>
        locales.First(l => l.IsMaster).Iso2Code;

    private void EnsureMasterMatchesConfig(string master)
    {
        if (!_hasExplicitDefault || string.Equals(master, _appOptions.DefaultLocale, StringComparison.Ordinal))
            return;

        throw new InvalidOperationException(
            $"Squidex app '{_appOptions.AppName}': configured DefaultLocale '{_appOptions.DefaultLocale}' " +
            $"does not match Squidex's actual master locale '{master}'.");
    }

    private LocaleState Compose(string defaultLocale, IReadOnlyList<string> supported) =>
        supported.Contains(defaultLocale)
            ? new LocaleState(defaultLocale, supported)
            : throw new InvalidOperationException(
                $"Squidex app '{_appOptions.AppName}': SupportedLocales " +
                $"[{string.Join(", ", supported)}] does not contain DefaultLocale '{defaultLocale}'.");

    private LocaleState Resolved() => _state ?? throw new InvalidOperationException(
        $"Squidex locales for app '{_appOptions.AppName}' are not resolved — " +
        "set DefaultLocale in configuration, or synchronise with Squidex at startup.");
}
