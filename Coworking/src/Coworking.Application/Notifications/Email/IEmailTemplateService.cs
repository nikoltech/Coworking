using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Notifications.Email;

public interface IEmailTemplateService
{
    string GetTemplateFromHbsFile(string templateFileName);
    string RenderTemplate(string templateContent, object model);
    string RenderTemplateFromHbsFile(string templateFileName, object model);
}
