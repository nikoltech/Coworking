using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;

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
            // Initialize Squidex locales once before serving requests
            await InitializeSquidexLocalesAsync(services, app);
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
    /// <returns></returns>
    private static async Task InitializeSquidexLocalesAsync(IServiceProvider services, WebApplication webApp)
    {
        var localeProvider = services.GetRequiredService<SquidexLocaleProvider>();
        var squidexClient = services.GetRequiredService<SquidexClientFactory>().Create();

        await localeProvider.InitializeAsync(squidexClient, webApp.Lifetime.ApplicationStopping);
    }
}