using Coworking.Application.Ports.Languages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace Coworking.Infrastructure.Providers;

public class LanguageProvider(IHttpContextAccessor accessor, IOptions<LanguageOptions> options)
    : ILanguageProvider
{
    public string CurrentLanguage =>
        accessor.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name
        ?? options.Value.Default;
}
