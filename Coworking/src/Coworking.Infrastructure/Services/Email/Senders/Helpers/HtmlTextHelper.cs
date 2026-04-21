using System.Net;
using System.Text.RegularExpressions;

namespace Coworking.Infrastructure.Services.Email.Senders.Helpers;

internal static class HtmlTextHelper
{
    public static string ToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = html;

        // Convert list items to bullet points
        text = Regex.Replace(
            text,
            @"<li[^>]*>",
            "• ",
            RegexOptions.IgnoreCase);

        // Preserve meaningful line breaks
        text = Regex.Replace(
            text,
            @"<(br|br/)\s*>",
            "\n",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"</p\s*>",
            "\n\n",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"</div\s*>",
            "\n",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"</li\s*>",
            "\n",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"</h[1-6]\s*>",
            "\n\n",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"</tr\s*>",
            "\n",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"</td\s*>",
            "\t",
            RegexOptions.IgnoreCase);

        // Remove remaining HTML tags
        text = Regex.Replace(
            text,
            @"<[^>]+>",
            string.Empty);

        // Decode HTML entities
        text = WebUtility.HtmlDecode(text);

        // Normalize line endings
        text = text.Replace("\r", string.Empty);

        // Collapse excessive blank lines
        text = Regex.Replace(
            text,
            @"\n{3,}",
            "\n\n");

        // Trim trailing spaces on each line
        var lines = text
            .Split('\n')
            .Select(line => line.TrimEnd());

        return string.Join('\n', lines).Trim();
    }
}