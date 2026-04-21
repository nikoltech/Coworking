using Coworking.Application.Notifications.Email;
using HandlebarsDotNet;

namespace Coworking.Infrastructure.Services.Email.Services;

internal class EmailTemplateService : IEmailTemplateService
{
    private readonly string _templateBasePath =
        Path.Combine(AppContext.BaseDirectory, "Email", "Templates");

    public string RenderTemplateFromHbsFile(string templateFileName, object model)
    {
        var templateContent = GetTemplateFromHbsFile(templateFileName);
        var template = Handlebars.Compile(templateContent);
        return template(model);
    }

    public string GetTemplateFromHbsFile(string templateFileName)
    {
        var fullPath = Path.Combine(_templateBasePath, templateFileName);

        if (File.Exists(fullPath))
            return File.ReadAllText(fullPath);

        throw new FileNotFoundException($"Template not found: {fullPath}");

    }

    public virtual string RenderTemplate(string templateContent, object model)
    {
        var template = Handlebars.Compile(templateContent);
        return template(model);
    }
}
