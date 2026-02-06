using System.Threading;
using Markdig;

namespace MoonBuggy;

/// <summary>
/// Static entry point for per-request i18n state and markdown configuration.
/// </summary>
public static class I18n
{
    private static readonly AsyncLocal<I18nContext> _current = new();

    /// <summary>
    /// Per-async-context i18n state. Lazily initialized — reading without
    /// setting returns a default context (LCID 0 = source locale).
    /// </summary>
    public static I18nContext Current
    {
        get => _current.Value ??= new I18nContext();
        set => _current.Value = value;
    }

    /// <summary>
    /// Markdig pipeline used for _m() markdown-to-HTML conversion
    /// in the runtime fallback path.
    /// </summary>
    public static MarkdownPipeline MarkdownPipeline { get; set; }
        = new MarkdownPipelineBuilder().Build();
}
