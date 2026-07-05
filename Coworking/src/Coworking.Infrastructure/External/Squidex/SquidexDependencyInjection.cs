using Coworking.Application.Ports.Squidex;
using Coworking.Application.Ports.Squidex.Clients;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Client;
using Coworking.Infrastructure.External.Squidex.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.Infrastructure.External.Squidex;

internal static class SquidexDependencyInjection
{
    internal static IServiceCollection AddCustomSquidexContexts(this IServiceCollection services)
    {
        services.AddMainAppContexts();

        return services;
    }

    private static IServiceCollection AddMainAppContexts(this IServiceCollection services)
    {
        const string AppName = MainSquidexContext.AppName;

        services.AddScoped<IMainSquidexContext>(sp =>
        {
            var factory = sp.GetRequiredService<SquidexClientFactory>();
            var paginator = sp.GetRequiredService<ISquidexPaginator>();
            var client = factory.CreateForApp(AppName);

            return new MainSquidexContext(client, paginator, factory);
        });

        services.AddKeyedScoped<IMainSquidexContext>(SquidexClientNames.Frontend, (sp, _) =>
        {
            var factory = sp.GetRequiredService<SquidexClientFactory>();
            var paginator = sp.GetRequiredService<ISquidexPaginator>();
            var client = factory.CreateForApp(AppName, SquidexClientNames.Frontend);

            return new MainSquidexContext(client, paginator, factory);
        });

        return services;
    }
}