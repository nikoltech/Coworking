using Coworking.Application.Ports.Synchronization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Coworking.IntegrationTests;

/// <summary>
/// Boots the real API against a separate database on the dev Postgres instance. Test classes
/// run in parallel and nothing is cleaned up between runs, so each one gets its own database.
/// </summary>
public sealed class TestApiFactory(
    bool bypassCoordinator,
    string database = "coworking_tests",
    Action<IServiceCollection>? configureServices = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development, so user secrets supply the real db/broker credentials
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var connectionString = config.Build().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // schema comes from EnsureCreated (current model), not from migration history
                ["ConnectionStrings:DefaultConnection"] = WithTestDatabase(connectionString),
                ["General:AutoMigrations"] = "false",
                ["General:SeedData"] = "false",
                ["General:ResetData"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // no broker in tests; the outbox still writes into the transaction
            RemoveMassTransitHostedServices(services);

            if (bypassCoordinator)
                services.Replace(
                    ServiceDescriptor.Singleton<IBookingAccessCoordinator, NoOpBookingAccessCoordinator>());

            configureServices?.Invoke(services);
        });
    }

    private string WithTestDatabase(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Database = database }.ConnectionString;

    private static void RemoveMassTransitHostedServices(IServiceCollection services)
    {
        var hosted = services
            .Where(d => d.ServiceType == typeof(IHostedService)
                     && d.ImplementationType?.Assembly.GetName().Name?.StartsWith("MassTransit") == true)
            .ToList();

        foreach (var descriptor in hosted)
            services.Remove(descriptor);
    }
}
