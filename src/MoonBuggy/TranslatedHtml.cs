using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace MoonBuggy;

/// <summary>
/// Zero-allocation translation result for <c>_m()</c> calls.
/// Contains pre-rendered HTML from Markdig. <see cref="WriteTo"/>
/// writes all segments directly without HTML-encoding (content is safe HTML).
/// </summary>
public sealed class TranslatedHtml : IHtmlContent
{
    private readonly string? _single;
    private readonly string?[]? _parts;

    /// <summary>Single-segment constructor (no variables).</summary>
    public TranslatedHtml(string value)
    {
        _single = value;
        _parts = null;
    }

    /// <summary>Multi-segment constructor (with variables).</summary>
    public TranslatedHtml(string?[] parts)
    {
        _single = null;
        _parts = parts;
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        if (_parts != null)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                var part = _parts[i];
                if (part != null)
                    writer.Write(part);
            }
        }
        else
        {
            writer.Write(_single ?? "");
        }
    }

    public override string ToString()
    {
        if (_parts != null)
        {
            return string.Concat(_parts);
        }
        return _single ?? "";
    }
}
