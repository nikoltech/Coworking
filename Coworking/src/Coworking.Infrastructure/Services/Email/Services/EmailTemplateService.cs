using Coworking.Application.Abstractions.Email;
using HandlebarsDotNet;
using LazyCache;

namespace Coworking.Infrastructure.Services.Email.Services;

internal sealed class EmailTemplateService(IAppCache cache) : IEmailTemplateService
{
    private const string FileTemplateCachePrefix = "email-template:file:";
    private const string RawTemplateCachePrefix = "email-template:raw:";

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private static readonly string TemplatesDirectory =
        Path.Combine(AppContext.BaseDirectory, "Services", "Email", "Templates");

    public async Task<string> RenderTemplateFromHbsFileAsync(string templateFileName, object model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateFileName);
        ArgumentNullException.ThrowIfNull(model);

        var sanitizedFileName = SanitizeFileName(templateFileName);
        var cacheKey = FileTemplateCachePrefix + sanitizedFileName;
        var localCache = cache;

        // Используем асинхронную версию кэша
        var template = await cache.GetOrAddAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;

            var content = await ReadTemplateFileAsync(localCache, sanitizedFileName);

            return Handlebars.Compile(content);
        });

        return template(model);
    }

    public async Task<string> GetTemplateFromHbsFileAsync(string templateFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateFileName);

        return await ReadTemplateFileAsync(cache, templateFileName);
    }

    public string RenderTemplate(string templateContent, object model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateContent);
        ArgumentNullException.ThrowIfNull(model);

        var template = Handlebars.Compile(templateContent);

        return template(model);
    }

    private static async Task<string> ReadTemplateFileAsync(IAppCache localCache, string templateFileName)
    {
        var sanitizedFileName = SanitizeFileName(templateFileName);
        var cacheKey = RawTemplateCachePrefix + sanitizedFileName;

        return await localCache.GetOrAddAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheLifetime;

            var fullPath = Path.Combine(
                TemplatesDirectory,
                Path.GetFileName(sanitizedFileName));

            if (File.Exists(fullPath) == false)
                throw new FileNotFoundException($"Template file was not found: {fullPath}");

            return await File.ReadAllTextAsync(fullPath);
        });
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        var invalidChars = Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        cleanName = Path.GetFileName(cleanName);

        if (cleanName.EndsWith(".hbs", StringComparison.OrdinalIgnoreCase) == false)
        {
            cleanName += ".hbs";
        }

        return cleanName;
    }
}