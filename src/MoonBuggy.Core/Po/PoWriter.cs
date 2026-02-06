using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MoonBuggy.Core.Po;

public static class PoWriter
{
    public static string Write(PoCatalog catalog)
    {
        var sb = new StringBuilder();
        using (var writer = new StringWriter(sb))
        {
            Write(catalog, writer);
        }
        return sb.ToString();
    }

    public static void Write(PoCatalog catalog, TextWriter writer)
    {
        var first = true;

        if (catalog.Header != null)
        {
            WriteEntry(writer, catalog.Header);
            first = false;
        }

        foreach (var entry in catalog.Entries)
        {
            if (!first)
                writer.WriteLine();
            WriteEntry(writer, entry);
            first = false;
        }
    }

    private static void WriteEntry(TextWriter writer, PoEntry entry)
    {
        foreach (var comment in entry.TranslatorComments)
            writer.WriteLine("# " + comment);

        foreach (var comment in entry.ExtractedComments)
            writer.WriteLine("#. " + comment);

        foreach (var reference in entry.References)
            writer.WriteLine("#: " + reference);

        foreach (var flag in entry.Flags)
            writer.WriteLine("#, " + flag);

        if (entry.MsgCtxt != null)
            WriteField(writer, "msgctxt", entry.MsgCtxt);

        WriteField(writer, "msgid", entry.MsgId);
        WriteField(writer, "msgstr", entry.MsgStr);
    }

    private static void WriteField(TextWriter writer, string keyword, string value)
    {
        var escaped = Escape(value);

        if (escaped.Contains("\\n"))
        {
            // Multiline: empty first line, then segments split on \n
            writer.WriteLine(keyword + " \"\"");
            var segments = SplitOnEscapedNewlines(escaped);
            for (var i = 0; i < segments.Count; i++)
                writer.WriteLine("\"" + segments[i] + "\"");
        }
        else
        {
            writer.WriteLine(keyword + " \"" + escaped + "\"");
        }
    }

    private static List<string> SplitOnEscapedNewlines(string escaped)
    {
        var segments = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < escaped.Length; i++)
        {
            current.Append(escaped[i]);
            if (escaped[i] == '\\' && i + 1 < escaped.Length && escaped[i + 1] == 'n')
            {
                current.Append('n');
                i++;
                segments.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
            segments.Add(current.ToString());

        return segments;
    }

    internal static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
