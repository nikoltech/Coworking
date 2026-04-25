using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.Options;
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
            .AddOptions<SquidexOptions>()
            .Bind(configuration.GetSection(SquidexOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();

        services.AddSingleton<SquidexTokenService>();
        services.AddSingleton<SquidexPaginator>();
        services.AddSingleton<SquidexLocaleProvider>();
        services.AddSingleton<SquidexClientFactory>();

        services.AddTransient<SquidexAuthHandler>();

        // Main client — auth handler in the pipeline
        services
            .AddHttpClient(SquidexHttpClientNames.Api)
            .AddHttpMessageHandler<SquidexAuthHandler>();

        // Auth client — no handler to avoid circular token fetch
        services.AddHttpClient(SquidexHttpClientNames.Auth);

        return services;
    }
}