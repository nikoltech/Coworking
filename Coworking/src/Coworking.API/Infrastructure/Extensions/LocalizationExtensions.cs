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

    public static WebApplication UseAppLocalization(this WebApplication app)
    {
        var languages = app.Services.GetRequiredService<IOptions<LanguageOptions>>().Value;

        var supported = languages.Supported
            .Select(ToCulture)
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

    private static CultureInfo ToCulture(string code)
    {
        try
        {
            return CultureInfo.GetCultureInfo(code, predefinedOnly: true);
        }
        catch (CultureNotFoundException ex)
        {
            throw new InvalidOperationException(
                $"{LanguageOptions.SectionName}:Supported holds '{code}', which is not a known culture.", ex);
        }
    }
}
