using System.IO;
using System.Text;

namespace MoonBuggy.Core.Po;

public static class PoReader
{
    public static PoCatalog Read(string poContent)
    {
        using (var reader = new StringReader(poContent))
        {
            return Read(reader);
        }
    }

    public static PoCatalog Read(TextReader reader)
    {
        var catalog = new PoCatalog();
        var entry = new PoEntry();
        var currentField = Field.None;
        var hasContent = false;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (hasContent)
                {
                    FinalizeEntry(catalog, entry);
                    entry = new PoEntry();
                    currentField = Field.None;
                    hasContent = false;
                }
                continue;
            }

            // Obsolete entries — skip
            if (line.StartsWith("#~"))
                continue;

            // Comments
            if (line.StartsWith("#"))
            {
                ParseComment(line, entry);
                hasContent = true;
                continue;
            }

            // Continuation line (starts with ")
            if (line.StartsWith("\""))
            {
                var value = ParseQuotedString(line);
                AppendToField(entry, currentField, value);
                hasContent = true;
                continue;
            }

            // Keyword lines
            if (line.StartsWith("msgctxt "))
            {
                currentField = Field.MsgCtxt;
                entry.MsgCtxt = ParseQuotedString(line.Substring("msgctxt ".Length));
                hasContent = true;
            }
            else if (line.StartsWith("msgid "))
            {
                currentField = Field.MsgId;
                entry.MsgId = ParseQuotedString(line.Substring("msgid ".Length));
                hasContent = true;
            }
            else if (line.StartsWith("msgstr "))
            {
                currentField = Field.MsgStr;
                entry.MsgStr = ParseQuotedString(line.Substring("msgstr ".Length));
                hasContent = true;
            }
        }

        // Finalize last entry if any
        if (hasContent)
            FinalizeEntry(catalog, entry);

        return catalog;
    }

    private static void FinalizeEntry(PoCatalog catalog, PoEntry entry)
    {
        if (entry.MsgId == "")
            catalog.Header = entry;
        else
            catalog.Entries.Add(entry);
    }

    private static void ParseComment(string line, PoEntry entry)
    {
        if (line.StartsWith("#. "))
            entry.ExtractedComments.Add(line.Substring(3));
        else if (line.StartsWith("#: "))
            entry.References.Add(line.Substring(3));
        else if (line.StartsWith("#, "))
            entry.Flags.Add(line.Substring(3));
        else if (line.StartsWith("# "))
            entry.TranslatorComments.Add(line.Substring(2));
    }

    private static void AppendToField(PoEntry entry, Field field, string value)
    {
        switch (field)
        {
            case Field.MsgCtxt:
                entry.MsgCtxt = (entry.MsgCtxt ?? "") + value;
                break;
            case Field.MsgId:
                entry.MsgId += value;
                break;
            case Field.MsgStr:
                entry.MsgStr += value;
                break;
        }
    }

    internal static string ParseQuotedString(string quoted)
    {
        quoted = quoted.Trim();
        if (quoted.Length < 2 || quoted[0] != '"' || quoted[quoted.Length - 1] != '"')
            return quoted;

        var inner = quoted.Substring(1, quoted.Length - 2);
        return Unescape(inner);
    }

    private static string Unescape(string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                i++;
                switch (value[i])
                {
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    default:
                        sb.Append('\\');
                        sb.Append(value[i]);
                        break;
                }
            }
            else
            {
                sb.Append(value[i]);
            }
        }
        return sb.ToString();
    }

    private enum Field
    {
        None,
        MsgCtxt,
        MsgId,
        MsgStr
    }
}
