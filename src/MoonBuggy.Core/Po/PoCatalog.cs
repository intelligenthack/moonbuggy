using System.Collections.Generic;

namespace MoonBuggy.Core.Po;

public class PoCatalog
{
    public PoEntry? Header { get; set; }
    public List<PoEntry> Entries { get; } = new List<PoEntry>();

    public PoEntry? Find(string msgId, string? msgCtxt = null)
    {
        foreach (var entry in Entries)
        {
            if (entry.MsgId == msgId && entry.MsgCtxt == msgCtxt)
                return entry;
        }
        return null;
    }

    public PoEntry GetOrAdd(string msgId, string? msgCtxt = null)
    {
        var existing = Find(msgId, msgCtxt);
        if (existing != null)
            return existing;

        var entry = new PoEntry { MsgId = msgId, MsgCtxt = msgCtxt };
        Entries.Add(entry);
        return entry;
    }

    public int RemoveObsolete(ISet<(string MsgId, string? MsgCtxt)> activeKeys)
    {
        var removed = Entries.RemoveAll(e => !activeKeys.Contains((e.MsgId, e.MsgCtxt)));
        return removed;
    }
}
