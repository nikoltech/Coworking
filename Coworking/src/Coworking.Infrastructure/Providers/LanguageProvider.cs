using Coworking.Application.Abstractions.Languages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Coworking.Infrastructure.Providers;

public class LanguageProvider(IHttpContextAccessor accessor) : ILanguageProvider
{
    public string CurrentLanguage
    {
        get
        {
            throw new NotImplementedException("LanguageProvider.CurrentLanguage has not configured yet. ");
            // TODO: implement language detection based on Accept-Language header or user preferences.
            // See webApplication.UseRequestLocalization in Program.cs for supported cultures setup.
            // SquidexLocaleProvider can be used as a reference for supported locales and default locale.

            var feature = accessor.HttpContext?.Features.Get<IRequestCultureFeature>();
            return feature?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "uk";
        }
    }
}
