using System.Collections.Generic;

namespace MoonBuggy.Core.Markdown;

public class PlaceholderMapping
{
    public int Index { get; set; }
    public string OpenTag { get; set; } = "";
    public string CloseTag { get; set; } = "";
}

public class MarkdownExtractionResult
{
    public string Text { get; set; } = "";
    public List<PlaceholderMapping> Mappings { get; set; } = new List<PlaceholderMapping>();
    public int NextIndex { get; set; }
}
