using Coworking.External.Squidex.Localization;
using Coworking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

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

            // Requires AddSquidex() enabled in Coworking.Infrastructure.DependencyInjection —
            // currently disabled there, so this stays commented out until Squidex is wired into DI.
            // await SquidexLocaleInitializer.InitializeAllAsync(services, app.Lifetime.ApplicationStopping);
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

        var logger = webApplication.Logger;

        using var scope = webApplication.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred while migrating or initializing the database. Exception: {@ex}", ex);
        }

        try
        {
            // === DEV SEED (remove me) ===
            var stopping = webApplication.Lifetime.ApplicationStopping;
            if (configuration.GetValue<bool>("General:ResetData"))
                await DevDataSeeder.ResetAsync(context, stopping);

            if (configuration.GetValue<bool>("General:SeedData"))
                await DevDataSeeder.SeedAsync(context, stopping);
            // === /DEV SEED ===
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred while seeding the database. Exception: {@ex}", ex);
        }
    }
}
