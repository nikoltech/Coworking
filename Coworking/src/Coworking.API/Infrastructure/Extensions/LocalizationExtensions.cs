using Coworking.Application.Ports.Languages;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Coworking.API.Infrastructure.Extensions;

public static class LocalizationExtensions
{
    public static IServiceCollection AddAppLocalization(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LanguageOptions>()
            .Bind(configuration.GetSection(LanguageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.Supported.Contains(options.Default),
                $"{LanguageOptions.SectionName}:Default must be listed in {LanguageOptions.SectionName}:Supported.")
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Culture and UI culture are kept equal: emails format dates in the language they are written in.
    /// </summary>
    public static WebApplication UseAppLocalization(this WebApplication app)
    {
        var languages = app.Services.GetRequiredService<IOptions<LanguageOptions>>().Value;

        // unknown codes throw here rather than silently resolving to the default later
        var supported = languages.Supported
            .Select(CultureInfo.GetCultureInfo)
            .ToArray();

        var options = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(languages.Default),
            SupportedCultures = supported,
            SupportedUICultures = supported,
            ApplyCurrentCultureToResponseHeaders = true,

            RequestCultureProviders = 
            [
                new QueryStringRequestCultureProvider(), 
                new AcceptLanguageHeaderRequestCultureProvider()
            ]
        };

        app.UseRequestLocalization(options);

        return app;
    }
}
