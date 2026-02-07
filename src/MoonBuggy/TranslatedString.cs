using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace MoonBuggy;

/// <summary>
/// Zero-allocation translation result for <c>_t()</c> calls.
/// In Razor, implements <see cref="IHtmlContent"/> so the view engine
/// calls <see cref="WriteTo"/> directly — no intermediate string allocation.
/// Variable values are HTML-encoded; developer-authored literals are not.
/// </summary>
public readonly struct TranslatedString : IHtmlContent
{
    private readonly string? _single;
    private readonly string?[]? _parts;
    private readonly bool[]? _encode;

    /// <summary>Single-segment constructor (no variables).</summary>
    public TranslatedString(string value)
    {
        _single = value;
        _parts = null;
        _encode = null;
    }

    /// <summary>Multi-segment constructor (with variables).</summary>
    public TranslatedString(string?[] parts, bool[] encode)
    {
        _single = null;
        _parts = parts;
        _encode = encode;
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        if (_parts != null)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                var part = _parts[i];
                if (part == null) continue;

                if (_encode != null && _encode[i])
                    encoder.Encode(writer, part);
                else
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

    public static implicit operator string(TranslatedString ts) => ts.ToString();
}
