namespace Coworking.Infrastructure.Services.Email;

public interface IEmailTemplateService
{
    Task<string> GetTemplateFromHbsFileAsync(string templateFileName);
    Task<string> RenderTemplateFromHbsFileAsync(string templateFileName, object model);
    string RenderTemplate(string templateContent, object model);
}
