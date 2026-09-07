using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Coworking.External.Squidex.Localization;

/// <summary>
/// Startup entry points for locale synchronisation and validation.
/// Both read /apps/{app}/languages, which no built-in role below Owner may call.
/// </summary>
public static class SquidexLocaleSync
{
    /// <summary>Fills in whatever configuration did not pin, for every configured app.</summary>
    /// <remarks>Needs Owner or a role granting languages.read; anything less fails with 403.</remarks>
    public static Task SyncAllAsync(IServiceProvider services, CancellationToken ct = default) =>
        ForEachAppAsync(services, (provider, client) => provider.SyncAsync(client, ct), ct);

    /// <summary>Checks every app's configuration against Squidex. Changes nothing.</summary>
    /// <remarks>Needs Owner or a role granting languages.read; anything less fails with 403.</remarks>
    public static Task ValidateAllAsync(IServiceProvider services, CancellationToken ct = default) =>
        ForEachAppAsync(services, (provider, client) => provider.ValidateAsync(client, ct), ct);

    private static async Task ForEachAppAsync(
        IServiceProvider services,
        Func<SquidexLocaleProvider, ISquidexApiClient, Task> action,
        CancellationToken ct)
    {
        var localeCache = services.GetRequiredService<SquidexLocaleProviderCache>();
        var clientFactory = services.GetRequiredService<SquidexClientFactory>();
        var globalOptions = services.GetRequiredService<IOptions<SquidexGlobalOptions>>();

        foreach (var (appName, appOptions) in globalOptions.Value.Apps)
        {
            ct.ThrowIfCancellationRequested();

            var client = clientFactory.CreateForApp(appName);
            var provider = localeCache.GetOrCreate(appName, appOptions);

            await action(provider, client);
        }
    }
}
