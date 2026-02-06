using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using MoonBuggy.Core.Parsing;

namespace MoonBuggy;

/// <summary>
/// Translation entry points. Consumed via <c>using static MoonBuggy.Translate;</c>.
/// Method bodies provide runtime fallback behavior (source-locale only).
/// When the source generator is active, interceptors bypass these entirely.
/// </summary>
public static class Translate
{
    public static string _t(
        [ConstantExpected] string message,
        object? args = null,
        [ConstantExpected] string? context = null)
    {
        var tokens = MbParser.Parse(message);
        return MbRenderer.Render(tokens, args);
    }

    public static IHtmlContent _m(
        [ConstantExpected] string message,
        object? args = null,
        [ConstantExpected] string? context = null)
    {
        var tokens = MbParser.Parse(message);
        var html = MbRenderer.RenderMarkdown(tokens, args);
        return new HtmlString(html);
    }
}
