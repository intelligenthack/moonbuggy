using System.Collections.Generic;

namespace MoonBuggy.Core.Po;

public class PoEntry
{
    public string MsgId { get; set; } = "";
    public string MsgStr { get; set; } = "";
    public string? MsgCtxt { get; set; }
    public List<string> TranslatorComments { get; } = new List<string>();
    public List<string> ExtractedComments { get; } = new List<string>();
    public List<string> References { get; } = new List<string>();
    public List<string> Flags { get; } = new List<string>();
}
