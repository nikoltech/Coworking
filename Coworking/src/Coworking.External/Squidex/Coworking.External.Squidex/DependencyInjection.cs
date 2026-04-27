using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.Pagination;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.External.Squidex;

public static class DependencyInjection
{
    public static IServiceCollection AddSquidex(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SquidexGlobalOptions>()
            .Bind(configuration.GetSection(SquidexGlobalOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();

        services.AddSingleton<SquidexTokenService>();
        services.AddSingleton<SquidexLocaleProviderCache>();
        services.AddSingleton<SquidexPaginator>();
        services.AddSingleton<ISquidexPaginator>(sp =>
            sp.GetRequiredService<SquidexPaginator>());

        services.AddSingleton<SquidexClientFactory>();

        services.AddTransient<SquidexAuthHandler>();

        services
            .AddHttpClient(SquidexHttpClientNames.Api)
            .AddHttpMessageHandler<SquidexAuthHandler>();

        services.AddHttpClient(SquidexHttpClientNames.Auth);

        return services;
    }
}