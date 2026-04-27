using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;
using Microsoft.Extensions.Options;

namespace Coworking.API.Infrastructure.Extensions.Initialization;

public static class AppInitializationExtensions
{
    public static async Task InitializeApplicationAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<WebApplication>>();

        logger.LogInformation("Starting application initialization (Squidex, Global Configs)...");

        try
        {
            // Note: has not used yet.
            //// Initialize Squidex locales once before serving requests
            //await InitializeSquidexLocalesAsync(services, app, logger);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "FATAL: Application initialization failed. System cannot start.");
            throw;
        }

        logger.LogInformation("Application initialization completed.");
    }

    /// <summary>
    /// Initialize Squidex locales once before serving requests
    /// </summary>
    /// <param name="services"></param>
    /// <param name="webApp"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    private static async Task InitializeSquidexLocalesAsync(IServiceProvider services, WebApplication webApp, ILogger logger)
    {
        logger.LogInformation("Initializing Squidex locales...");

        var localeProvider = services.GetRequiredService<SquidexLocaleProvider>();
        var squidexClientFactory = services.GetRequiredService<SquidexClientFactory>();

        var globalSqOptions = services.GetRequiredService<IOptions<SquidexGlobalOptions>>();
        var squidexApps = globalSqOptions?.Value.Apps;

        if (squidexApps is not null)
        {
            foreach (var appOptions in squidexApps)
            {
                var app = appOptions.Value;
                var appName = app.AppName;

                logger.LogInformation("Initializing locales for Squidex app '{AppName}'...", appName);

                if (app.Clients is not null && app.Clients.Count > 0)
                {
                    var client = app.Clients.First();

                    var squidexClient = squidexClientFactory.CreateForApp(appName, client.Value?.ClientId);
                    await localeProvider.InitializeAsync(squidexClient, webApp.Lifetime.ApplicationStopping);
                }

                logger.LogInformation("Locales initialization for Squidex app '{AppName}' completed.", appName);
            }
        }

        logger.LogInformation("Squidex locales initialization completed.");
    }
}