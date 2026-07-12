using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace AMIS.Blazor.Services.Help;

/// <summary>
/// Loads user-guide markdown files (copied from <c>docs/user-guide</c> into <c>wwwroot/help</c> at build
/// time) and renders them to HTML for the in-app Help pages. Intentionally supports only the small
/// CommonMark subset the guides use (headings, paragraphs, bold/italic/code, links, images, ordered and
/// unordered lists, fenced code blocks, block quotes, tables, horizontal rules) so no external markdown
/// dependency is required.
/// </summary>
internal interface IHelpContentService
{
    /// <summary>Returns the rendered HTML for a guide slug (e.g. "procurement-cycle/00-overview"), or null if missing.</summary>
    Task<MarkupString?> RenderAsync(string slug, CancellationToken ct = default);
}

internal sealed partial class HelpContentService(IWebHostEnvironment env) : IHelpContentService
{
    private const string HelpRoot = "help";

    public async Task<MarkupString?> RenderAsync(string slug, CancellationToken ct = default)
    {
        var relative = NormalizeSlug(slug);
        if (relative is null)
        {
            return null;
        }

        // Confine reads to wwwroot/help — never traverse outside it.
        var helpDir = Path.GetFullPath(Path.Combine(env.WebRootPath, HelpRoot));
        var fullPath = Path.GetFullPath(Path.Combine(helpDir, relative + ".md"));
        if (!fullPath.StartsWith(helpDir, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return null;
        }

        var markdown = await File.ReadAllTextAsync(fullPath, ct);

        // The directory the article lives in, used to resolve its relative links/images.
        var slashIndex = relative.LastIndexOf('/');
        var baseDir = slashIndex < 0 ? string.Empty : relative[..slashIndex];

        return new MarkupString(ToHtml(markdown, baseDir));
    }

    /// <summary>
    /// Turns a link/image target that is relative to <paramref name="baseDir"/> into an absolute
    /// <c>/help/...</c> URL. This is required because the Blazor host declares <c>&lt;base href="/"&gt;</c>,
    /// which makes the browser resolve relative URLs against the site root rather than the current path —
    /// so a bare "02-canvass-riv" would otherwise navigate to "/02-canvass-riv" and 404.
    /// </summary>
    private static string ResolveHelpUrl(string raw, string baseDir, bool isImage)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || raw.StartsWith('/')
            || raw.StartsWith('#')
            || raw.Contains("://", StringComparison.Ordinal)
            || raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        // Split off any #anchor before manipulating the path.
        var anchorIndex = raw.IndexOf('#', StringComparison.Ordinal);
        var path = anchorIndex < 0 ? raw : raw[..anchorIndex];
        var anchor = anchorIndex < 0 ? string.Empty : raw[anchorIndex..];

        // Guide links point at .md files; the in-app route has no extension.
        if (!isImage && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^3];
        }

        // Resolve "." and ".." against the article's directory.
        var segments = new List<string>();
        if (!string.IsNullOrEmpty(baseDir))
        {
            segments.AddRange(baseDir.Split('/', StringSplitOptions.RemoveEmptyEntries));
        }

        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }
                continue;
            }

            segments.Add(part);
        }

        return $"/{HelpRoot}/{string.Join('/', segments)}{anchor}";
    }

    /// <summary>Strips leading/trailing slashes, rejects traversal, and enforces forward slashes.</summary>
    private static string? NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var cleaned = slug.Replace('\\', '/').Trim('/');
        if (cleaned.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(cleaned))
        {
            return null;
        }

        return cleaned;
    }

    // ── Minimal markdown → HTML ────────────────────────────────────────────────────────────────────
    private static string ToHtml(string markdown, string baseDir)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var sb = new StringBuilder("<div class=\"help-article\">");
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // Fenced code block
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                i++;
                var code = new StringBuilder();
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    code.AppendLine(Escape(lines[i]));
                    i++;
                }
                i++; // closing fence
                sb.Append("<pre class=\"help-pre\"><code>").Append(code).Append("</code></pre>");
                continue;
            }

            // Blank line
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Horizontal rule
            if (Regex.IsMatch(line, "^\\s*([-*_])\\1\\1[-*_\\s]*$"))
            {
                sb.Append("<hr />");
                i++;
                continue;
            }

            // Heading
            var heading = Regex.Match(line, "^(#{1,6})\\s+(.*)$");
            if (heading.Success)
            {
                var level = (char)('0' + heading.Groups[1].Value.Length);
                sb.Append("<h").Append(level).Append('>')
                  .Append(Inline(heading.Groups[2].Value, baseDir))
                  .Append("</h").Append(level).Append('>');
                i++;
                continue;
            }

            // Block quote (one or more consecutive '>' lines)
            if (line.TrimStart().StartsWith('>'))
            {
                var quote = new StringBuilder();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('>'))
                {
                    quote.Append(Inline(Regex.Replace(lines[i].TrimStart(), "^>\\s?", ""), baseDir)).Append(' ');
                    i++;
                }
                sb.Append("<blockquote>").Append(quote.ToString().Trim()).Append("</blockquote>");
                continue;
            }

            // Table (header row followed by a |---|---| separator)
            if (line.Contains('|', StringComparison.Ordinal) && i + 1 < lines.Length && Regex.IsMatch(lines[i + 1], "^\\s*\\|?\\s*:?-{2,}"))
            {
                i = AppendTable(sb, lines, i, baseDir);
                continue;
            }

            // Ordered list
            if (Regex.IsMatch(line, "^\\s*\\d+\\.\\s+"))
            {
                sb.Append("<ol>");
                while (i < lines.Length && Regex.IsMatch(lines[i], "^\\s*\\d+\\.\\s+"))
                {
                    sb.Append("<li>").Append(Inline(Regex.Replace(lines[i], "^\\s*\\d+\\.\\s+", ""), baseDir)).Append("</li>");
                    i++;
                }
                sb.Append("</ol>");
                continue;
            }

            // Unordered list
            if (Regex.IsMatch(line, "^\\s*[-*]\\s+"))
            {
                sb.Append("<ul>");
                while (i < lines.Length && Regex.IsMatch(lines[i], "^\\s*[-*]\\s+"))
                {
                    sb.Append("<li>").Append(Inline(Regex.Replace(lines[i], "^\\s*[-*]\\s+", ""), baseDir)).Append("</li>");
                    i++;
                }
                sb.Append("</ul>");
                continue;
            }

            // Paragraph (gather until blank/structural line)
            var para = new StringBuilder();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])
                   && !Regex.IsMatch(lines[i], "^\\s*(#{1,6}\\s|[-*]\\s|\\d+\\.\\s|>|```)")
                   && !Regex.IsMatch(lines[i], "^\\s*([-*_])\\1\\1"))
            {
                if (para.Length > 0)
                {
                    para.Append(' ');
                }
                para.Append(Inline(lines[i], baseDir));
                i++;
            }
            if (para.Length > 0)
            {
                sb.Append("<p>").Append(para).Append("</p>");
            }
        }

        return sb.Append("</div>").ToString();
    }

    private static int AppendTable(StringBuilder sb, string[] lines, int i, string baseDir)
    {
        static string[] Cells(string row) =>
            row.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();

        var headers = Cells(lines[i]);
        i += 2; // skip header + separator
        sb.Append("<table class=\"help-table\"><thead><tr>");
        foreach (var h in headers)
        {
            sb.Append("<th>").Append(Inline(h, baseDir)).Append("</th>");
        }
        sb.Append("</tr></thead><tbody>");

        while (i < lines.Length && lines[i].Contains('|', StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(lines[i]))
        {
            sb.Append("<tr>");
            foreach (var c in Cells(lines[i]))
            {
                sb.Append("<td>").Append(Inline(c, baseDir)).Append("</td>");
            }
            sb.Append("</tr>");
            i++;
        }
        sb.Append("</tbody></table>");
        return i;
    }

    /// <summary>Inline formatting: images, links, bold, italic, code. Links to *.md become in-app routes.</summary>
    private static string Inline(string text, string baseDir)
    {
        text = Escape(text);

        // Images: ![alt](src) — rewritten to an absolute /help/... URL (see ResolveHelpUrl).
        text = ImageRegex().Replace(text, m =>
        {
            var src = ResolveHelpUrl(m.Groups[2].Value, baseDir, isImage: true);
            return $"<img class=\"help-img\" src=\"{src}\" alt=\"{m.Groups[1].Value}\" loading=\"lazy\" />";
        });

        // Links: [text](href) — rewritten to an absolute /help/... route, dropping the .md extension.
        text = LinkRegex().Replace(text, m =>
        {
            var href = ResolveHelpUrl(m.Groups[2].Value, baseDir, isImage: false);
            return $"<a href=\"{href}\">{m.Groups[1].Value}</a>";
        });

        text = BoldRegex().Replace(text, "<strong>$1</strong>");
        text = ItalicRegex().Replace(text, "<em>$1</em>");
        text = CodeRegex().Replace(text, "<code>$1</code>");
        return text;
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
         .Replace("<", "&lt;", StringComparison.Ordinal)
         .Replace(">", "&gt;", StringComparison.Ordinal);

    [GeneratedRegex("!\\[([^\\]]*)\\]\\(([^)]+)\\)")] private static partial Regex ImageRegex();
    [GeneratedRegex("\\[([^\\]]+)\\]\\(([^)]+)\\)")] private static partial Regex LinkRegex();
    [GeneratedRegex("\\*\\*([^*]+)\\*\\*")] private static partial Regex BoldRegex();
    [GeneratedRegex("(?<!\\*)\\*(?!\\*)([^*]+)\\*(?!\\*)")] private static partial Regex ItalicRegex();
    [GeneratedRegex("`([^`]+)`")] private static partial Regex CodeRegex();
}
