using Coworking.External.Squidex.Abstractions.Context;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Context;
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
            .Validate(o => string.IsNullOrEmpty(o.DefaultApp) || o.Apps.ContainsKey(o.DefaultApp),
                "Squidex: DefaultApp must be one of the configured Apps.")
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

        RegisterContexts(services, configuration);

        return services;
    }

    private static void RegisterContexts(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(SquidexGlobalOptions.SectionName)
            .Get<SquidexGlobalOptions>();

        var appNames = options?.Apps.Keys.ToList() ?? [];
        if (appNames.Count == 0)
            return;

        if (appNames.Count == 1)
        {
            services.AddScoped<ISquidexContext>(sp => CreateContext(sp, appNames[0]));
            return;
        }

        foreach (var appName in appNames)
            services.AddKeyedScoped<ISquidexContext>(appName, (sp, key) => CreateContext(sp, (string)key!));

        if (!string.IsNullOrWhiteSpace(options!.DefaultApp))
            services.AddScoped<ISquidexContext>(sp => CreateContext(sp, options.DefaultApp));
    }

    private static SquidexContext CreateContext(IServiceProvider sp, string appName)
    {
        var factory = sp.GetRequiredService<SquidexClientFactory>();
        var paginator = sp.GetRequiredService<ISquidexPaginator>();

        return new SquidexContext(factory.CreateForApp(appName), paginator, factory, appName);
    }
}