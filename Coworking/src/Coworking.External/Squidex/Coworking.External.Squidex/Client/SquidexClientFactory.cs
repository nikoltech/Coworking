using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Localization;
using Microsoft.Extensions.Options;

namespace Coworking.External.Squidex.Client;

/// <summary>
/// Creates SquidexApiClient instances for app+client combinations.
/// Locale providers are cached per app to avoid redundant Squidex API calls.
/// </summary>
public sealed class SquidexClientFactory(
    IHttpClientFactory httpClientFactory,
    IOptions<SquidexGlobalOptions> globalOptions,
    SquidexLocaleProviderCache localeCache)
{
    private readonly SquidexGlobalOptions _options = globalOptions.Value;

    public ISquidexApiClient CreateForApp(string appName, string? clientName = null)
    {
        var appOptions = GetAppOptions(appName);
        var client = clientName ?? appOptions.DefaultClient;
        var locales = localeCache.GetOrCreate(appName, appOptions);
        var http = httpClientFactory.CreateClient(SquidexHttpClientNames.Api);

        return new SquidexApiClient(http, appOptions, client, locales);
    }

    // ── private ──────────────────────────────────────────────────────────────

    private SquidexAppOptions GetAppOptions(string appName) =>
        _options.Apps.TryGetValue(appName, out var app)
            ? app
            : throw new InvalidOperationException(
                $"Squidex app '{appName}' is not configured. " +
                $"Available: {string.Join(", ", _options.Apps.Keys)}");
}