using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Coworking.API.Infrastructure.Extensions.Initialization;

public static class AppInitializationExtensions
{
    public static async Task InitializeApplicationAsync(this WebApplication app, IConfiguration config)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<WebApplication>>();

        logger.LogInformation("Starting application initialization (Squidex, Global Configs)...");

        try
        {
            await InitDatabase(config, app);

            // Note: has not used yet. Need testing
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

    public static async Task InitDatabase(IConfiguration configuration, WebApplication webApplication)
    {
        if (configuration.GetValue<bool>("General:AutoMigrations") is false)
        {
            return;
        }

        try
        {
            using var scope = webApplication.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            webApplication.Logger.LogError("An error occurred while migrating or initializing the database. Exception: {@ex}", ex);
        }
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

        throw new OperationCanceledException("Need testing external Squidex package");

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