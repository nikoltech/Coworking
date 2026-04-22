using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Abstractions.Email;

public interface IEmailTemplateService
{
    Task<string> GetTemplateFromHbsFileAsync(string templateFileName);
    Task<string> RenderTemplateFromHbsFileAsync(string templateFileName, object model);
    string RenderTemplate(string templateContent, object model);
}
