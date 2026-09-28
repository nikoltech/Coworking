using Coworking.External.Squidex.Abstractions.Context;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Context;
using Coworking.External.Squidex.Pagination;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.External.Squidex;

public static class DependencyInjection
{
    private static readonly string[] RemovedLocaleKeys = ["DefaultLocale", "SupportedLocales"];

    public static IServiceCollection AddSquidex(this IServiceCollection services, IConfiguration configuration)
    {
        EnsureNoLegacyLocaleKeys(configuration);

        services
            .AddOptions<SquidexGlobalOptions>()
            .Bind(configuration.GetSection(SquidexGlobalOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => string.IsNullOrEmpty(o.DefaultApp) || o.Apps.ContainsKey(o.DefaultApp),
                "Squidex: DefaultApp must be one of the configured Apps.")
            .Validate(o => o.Apps.Values.All(a => a.Clients.ContainsKey(a.DefaultClient)),
                "Squidex: DefaultClient must be one of the app's configured Clients.")
            .ValidateOnStart();

        services.AddMemoryCache();

        services.AddSingleton<SquidexTokenService>();
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

    // the binder ignores unknown keys, so a leftover key would look configured and do nothing
    private static void EnsureNoLegacyLocaleKeys(IConfiguration configuration)
    {
        var apps = configuration
            .GetSection($"{SquidexGlobalOptions.SectionName}:Apps")
            .GetChildren();

        var stale = apps
            .SelectMany(app => RemovedLocaleKeys
                .Where(key => app.GetSection(key).Exists())
                .Select(key => $"{app.Key}:{key}"))
            .ToList();

        if (stale.Count > 0)
            throw new InvalidOperationException(
                $"Squidex: locales are no longer configured — remove {string.Join(", ", stale)}. " +
                "Reads return every locale; narrow a single call with QueryOptions.Languages.");
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