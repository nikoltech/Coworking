using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Coworking.External.Squidex.Localization;

/// <summary>Always synchronizes locales with Squidex for every configured app.</summary>
public static class SquidexLocaleInitializer
{
    public static async Task InitializeAllAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SquidexLocaleInitializer).FullName!);
        var localeCache = services.GetRequiredService<SquidexLocaleProviderCache>();
        var clientFactory = services.GetRequiredService<SquidexClientFactory>();
        var globalOptions = services.GetRequiredService<IOptions<SquidexGlobalOptions>>();

        foreach (var (appName, appOptions) in globalOptions.Value.Apps)
        {
            logger.LogInformation("Initializing locales for Squidex app '{AppName}'...", appName);

            if (appOptions.Clients.Count == 0)
            {
                logger.LogWarning("Squidex app '{AppName}' has no configured clients — skipping locale init.", appName);
                continue;
            }

            // Locales are app-level metadata, not client-specific — any configured client works.
            var clientName = appOptions.Clients.Keys.First();
            var client = clientFactory.CreateForApp(appName, clientName);

            var localeProvider = localeCache.GetOrCreate(appName, appOptions);
            await localeProvider.InitializeAsync(client, ct);

            logger.LogInformation("Locales initialization for Squidex app '{AppName}' completed.", appName);
        }
    }
}
