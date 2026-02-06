using System.Collections.Generic;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MoonBuggy.Core.Parsing;

namespace MoonBuggy.Core.Markdown;

public static class MarkdownPlaceholderExtractor
{
    private const char Sentinel = '\uFFF2';

    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().Build();

    /// <summary>
    /// Replace inline markdown in text with &lt;N&gt;...&lt;/N&gt; placeholders.
    /// Text may contain {var} and sentinel characters (for ICU #).
    /// </summary>
    public static MarkdownExtractionResult Extract(string text, int startIndex = 0)
    {
        var doc = Markdig.Parsers.MarkdownParser.Parse(text, Pipeline);

        var sb = new StringBuilder();
        var mappings = new List<PlaceholderMapping>();
        var index = startIndex;

        // Markdig wraps inline content in a ParagraphBlock
        foreach (var block in doc)
        {
            if (block is ParagraphBlock para && para.Inline != null)
            {
                WalkInlines(para.Inline, sb, mappings, ref index);
            }
        }

        return new MarkdownExtractionResult
        {
            Text = sb.ToString(),
            Mappings = mappings,
            NextIndex = index
        };
    }

    /// <summary>
    /// Full pipeline: MB syntax → ICU with markdown placeholders.
    /// </summary>
    public static string ToIcuWithMarkdown(string mbSyntax)
    {
        var tokens = MbParser.Parse(mbSyntax);
        var sb = new StringBuilder();
        var index = 0;
        AppendTokensWithMarkdown(tokens, sb, ref index);
        return sb.ToString();
    }

    private static void AppendTokensWithMarkdown(
        MbToken[] tokens, StringBuilder sb, ref int index)
    {
        // Step 1: Assemble text from tokens, replacing # with sentinel
        var assembled = new StringBuilder();
        foreach (var token in tokens)
        {
            switch (token)
            {
                case TextToken t:
                    assembled.Append(t.Value);
                    break;
                case VariableToken v:
                    assembled.Append('{');
                    assembled.Append(v.Name);
                    assembled.Append('}');
                    break;
                case PluralSelectorRef:
                    assembled.Append(Sentinel);
                    break;
                case PluralBlockToken p:
                    // Flush accumulated text through markdown extraction
                    FlushMarkdown(assembled, sb, ref index);
                    // Build the plural ICU structure with per-form markdown extraction
                    AppendPluralWithMarkdown(p, sb, ref index);
                    break;
            }
        }
        // Flush remaining text
        FlushMarkdown(assembled, sb, ref index);
    }

    private static void FlushMarkdown(StringBuilder assembled, StringBuilder sb, ref int index)
    {
        if (assembled.Length == 0)
            return;

        var text = assembled.ToString();
        assembled.Clear();

        // Markdig trims leading/trailing whitespace from paragraphs.
        // Preserve it by extracting and re-appending around the markdown result.
        var leadingSpaces = 0;
        while (leadingSpaces < text.Length && text[leadingSpaces] == ' ')
            leadingSpaces++;

        var trailingSpaces = 0;
        while (trailingSpaces < text.Length - leadingSpaces &&
               text[text.Length - 1 - trailingSpaces] == ' ')
            trailingSpaces++;

        var result = Extract(text, index);
        var resultText = result.Text.Replace(Sentinel.ToString(), "#");

        // Re-add any spaces that Markdig stripped
        sb.Append(' ', leadingSpaces - CountLeadingSpaces(resultText));
        sb.Append(resultText);
        var resultTrailing = CountTrailingSpaces(resultText);
        sb.Append(' ', trailingSpaces - resultTrailing);

        index = result.NextIndex;
    }

    private static int CountLeadingSpaces(string s)
    {
        var count = 0;
        while (count < s.Length && s[count] == ' ')
            count++;
        return count;
    }

    private static int CountTrailingSpaces(string s)
    {
        var count = 0;
        while (count < s.Length && s[s.Length - 1 - count] == ' ')
            count++;
        return count;
    }

    private static void AppendPluralWithMarkdown(
        PluralBlockToken plural, StringBuilder sb, ref int index)
    {
        sb.Append('{');
        sb.Append(plural.SelectorVariable);
        sb.Append(", plural,");

        foreach (var form in plural.Forms)
        {
            sb.Append(' ');
            sb.Append(form.Category);
            sb.Append(" {");

            // Assemble form content text, then run markdown extraction
            var formAssembled = new StringBuilder();
            foreach (var token in form.Content)
            {
                switch (token)
                {
                    case TextToken t:
                        formAssembled.Append(t.Value);
                        break;
                    case VariableToken v:
                        formAssembled.Append('{');
                        formAssembled.Append(v.Name);
                        formAssembled.Append('}');
                        break;
                    case PluralSelectorRef:
                        formAssembled.Append(Sentinel);
                        break;
                }
            }

            var formText = formAssembled.ToString();
            var result = Extract(formText, index);
            sb.Append(result.Text.Replace(Sentinel.ToString(), "#"));
            index = result.NextIndex;

            sb.Append('}');
        }

        sb.Append('}');
    }

    private static void WalkInlines(
        ContainerInline container, StringBuilder sb,
        List<PlaceholderMapping> mappings, ref int index)
    {
        var child = container.FirstChild;
        while (child != null)
        {
            switch (child)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content);
                    break;

                case EmphasisInline emphasis:
                {
                    var idx = index++;
                    var isStrong = emphasis.DelimiterCount >= 2;
                    var openTag = isStrong ? "<strong>" : "<em>";
                    var closeTag = isStrong ? "</strong>" : "</em>";

                    mappings.Add(new PlaceholderMapping
                    {
                        Index = idx,
                        OpenTag = openTag,
                        CloseTag = closeTag
                    });

                    sb.Append('<');
                    sb.Append(idx);
                    sb.Append('>');

                    WalkInlines(emphasis, sb, mappings, ref index);

                    sb.Append("</");
                    sb.Append(idx);
                    sb.Append('>');
                    break;
                }

                case CodeInline code:
                {
                    var idx = index++;
                    mappings.Add(new PlaceholderMapping
                    {
                        Index = idx,
                        OpenTag = "<code>",
                        CloseTag = "</code>"
                    });

                    sb.Append('<');
                    sb.Append(idx);
                    sb.Append('>');
                    sb.Append(code.Content);
                    sb.Append("</");
                    sb.Append(idx);
                    sb.Append('>');
                    break;
                }

                case LinkInline link:
                {
                    var idx = index++;
                    var url = link.Url ?? "";
                    mappings.Add(new PlaceholderMapping
                    {
                        Index = idx,
                        OpenTag = $"<a href=\"{url}\">",
                        CloseTag = "</a>"
                    });

                    sb.Append('<');
                    sb.Append(idx);
                    sb.Append('>');

                    WalkInlines(link, sb, mappings, ref index);

                    sb.Append("</");
                    sb.Append(idx);
                    sb.Append('>');
                    break;
                }

                case ContainerInline innerContainer:
                    WalkInlines(innerContainer, sb, mappings, ref index);
                    break;

                case LineBreakInline:
                    sb.Append('\n');
                    break;
            }

            child = child.NextSibling;
        }
    }
}
