using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;

namespace MoonBuggy;

/// <summary>
/// Translation entry points. Consumed via <c>using static MoonBuggy.Translate;</c>.
/// Method bodies throw at runtime — the source generator emits interceptors
/// that bypass these entirely. If no interceptor is active, the throw surfaces
/// a clear error instead of silently falling back to source-locale rendering.
/// </summary>
public static class Translate
{
    public static string _t(
        [ConstantExpected] string message,
        object? args = null,
        [ConstantExpected] string? context = null)
    {
        throw new InvalidOperationException(
            "MoonBuggy source generator is not active. Add the MoonBuggy.SourceGenerator package to your project.");
    }

    public static IHtmlContent _m(
        [ConstantExpected] string message,
        object? args = null,
        [ConstantExpected] string? context = null)
    {
        throw new InvalidOperationException(
            "MoonBuggy source generator is not active. Add the MoonBuggy.SourceGenerator package to your project.");
    }
}
