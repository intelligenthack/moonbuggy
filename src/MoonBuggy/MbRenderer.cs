using System.Text;
using Markdig;
using MoonBuggy.Core.Parsing;

namespace MoonBuggy;

/// <summary>
/// Renders MB tokens at runtime by resolving variables via reflection
/// and selecting plural forms via CLDR rules. Used by the fallback path only.
/// </summary>
internal static class MbRenderer
{
    internal static string Render(MbToken[] tokens, object? args)
    {
        var sb = new StringBuilder();
        RenderTokens(tokens, args, sb);
        return sb.ToString();
    }

    internal static string RenderMarkdown(MbToken[] tokens, object? args)
    {
        // Step 1: Resolve all MB tokens into plain text (with markdown still in it)
        var text = Render(tokens, args);

        // Step 2: Run Markdig to convert markdown → HTML
        var html = Markdown.ToHtml(text, I18n.MarkdownPipeline);

        // Markdig wraps output in <p>...</p>\n — unwrap for inline use
        html = UnwrapParagraph(html);

        return html;
    }

    private static string UnwrapParagraph(string html)
    {
        html = html.Trim();
        if (html.StartsWith("<p>") && html.EndsWith("</p>"))
        {
            html = html.Substring(3, html.Length - 7);
        }
        return html;
    }

    private static void RenderTokens(MbToken[] tokens, object? args, StringBuilder sb)
    {
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken t:
                    sb.Append(t.Value);
                    break;
                case VariableToken v:
                    sb.Append(ResolveVariable(v.Name, args));
                    break;
                case PluralSelectorRef s:
                    sb.Append(ResolveVariable(s.Name, args));
                    break;
                case PluralBlockToken p:
                    RenderPlural(p, args, sb);
                    break;
            }
        }
    }

    private static void RenderPlural(PluralBlockToken plural, object? args, StringBuilder sb)
    {
        var value = ResolveNumericVariable(plural.SelectorVariable, args);

        // Select the appropriate form
        PluralForm? selected = null;

        // Check =0 form first
        if (plural.HasZeroForm && value == 0)
        {
            selected = plural.Forms[0]; // =0 is always first
        }

        if (selected == null)
        {
            var category = Core.Plural.CldrPluralRules.GetCategory("en", value);
            // Map CLDR category to the correct form
            foreach (var form in plural.Forms)
            {
                if (form.Category == "=0") continue; // skip =0, already checked
                if (form.Category == "other")
                {
                    selected ??= form; // fallback
                }
                else if (form.Category == "one" && category == Core.Plural.PluralCategory.One)
                {
                    selected = form;
                    break;
                }
            }
        }

        if (selected != null)
        {
            RenderTokens(selected.Content, args, sb);
        }
    }

    private static string ResolveVariable(string name, object? args)
    {
        if (args == null) return "";
        var prop = args.GetType().GetProperty(name);
        return prop?.GetValue(args)?.ToString() ?? "";
    }

    private static long ResolveNumericVariable(string name, object? args)
    {
        if (args == null) return 0;
        var prop = args.GetType().GetProperty(name);
        if (prop == null) return 0;
        var val = prop.GetValue(args);
        return Convert.ToInt64(val);
    }
}
