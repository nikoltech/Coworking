using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Coworking.Infrastructure.Services.Email.Senders.Helpers;

internal static partial class HtmlTextHelper
{
    public static string ToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = html;

        // Convert list items to bullets
        text = LiTagRegex().Replace(text, "• ");

        // Preserve meaningful structure
        text = BrTagRegex().Replace(text, "\n");
        text = ParagraphCloseRegex().Replace(text, "\n\n");
        text = DivCloseRegex().Replace(text, "\n");
        text = ListItemCloseRegex().Replace(text, "\n");
        text = HeadingCloseRegex().Replace(text, "\n\n");
        text = TableRowCloseRegex().Replace(text, "\n");
        text = TableCellCloseRegex().Replace(text, "\t");

        // Remove remaining tags
        text = AnyTagRegex().Replace(text, string.Empty);

        // Decode HTML entities
        text = WebUtility.HtmlDecode(text);

        // Normalize line endings to LF
        text = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        // Collapse excessive blank lines
        text = ExcessiveNewLinesRegex().Replace(text, "\n\n");

        // Trim trailing spaces per line without Split/Join allocations
        text = TrimTrailingSpacesPerLine(text);

        return text.Trim();
    }

    private static string TrimTrailingSpacesPerLine(string text)
    {
        var sb = new StringBuilder(text.Length);
        var lineStart = 0;

        for (var i = 0; i <= text.Length; i++)
        {
            var isEnd = i == text.Length;
            var isNewLine = !isEnd && text[i] == '\n';

            if (!isEnd && !isNewLine)
                continue;

            var lineEnd = i;

            while (lineEnd > lineStart &&
                   (text[lineEnd - 1] == ' ' || text[lineEnd - 1] == '\t'))
            {
                lineEnd--;
            }

            sb.Append(text, lineStart, lineEnd - lineStart);

            if (!isEnd)
                sb.Append('\n');

            lineStart = i + 1;
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"<li\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LiTagRegex();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex(@"</p\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ParagraphCloseRegex();

    [GeneratedRegex(@"</div\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex DivCloseRegex();

    [GeneratedRegex(@"</li\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemCloseRegex();

    [GeneratedRegex(@"</h[1-6]\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingCloseRegex();

    [GeneratedRegex(@"</tr\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex TableRowCloseRegex();

    [GeneratedRegex(@"</td\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex TableCellCloseRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex AnyTagRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewLinesRegex();
}