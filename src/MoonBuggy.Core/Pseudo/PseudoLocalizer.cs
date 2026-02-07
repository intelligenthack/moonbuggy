using System.Text;
using MoonBuggy.Core.Icu;

namespace MoonBuggy.Core.Pseudo;

public static class PseudoLocalizer
{
    private const char RingAbove = '\u030A';
    private const char Diaeresis = '\u0308';
    private const char DotAbove = '\u0307';
    private const char Tilde = '\u0303';
    private const char Cedilla = '\u0327';
    private const char AcuteAccent = '\u0301';

    public static string Accent(char c)
    {
        if (!char.IsLetter(c))
            return c.ToString();

        var combining = GetCombiningChar(c);
        var seq = c.ToString() + combining;
        return seq.Normalize(NormalizationForm.FormC);
    }

    public static string Transform(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var nodes = IcuParser.Parse(input);
        var sb = new StringBuilder();
        TransformNodes(nodes, sb);
        return sb.ToString();
    }

    private static void TransformNodes(IcuNode[] nodes, StringBuilder sb)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case IcuTextNode text:
                    AccentText(text.Value, sb);
                    break;
                case IcuVariableNode variable:
                    sb.Append('{');
                    sb.Append(variable.Name);
                    sb.Append('}');
                    break;
                case IcuHashNode:
                    sb.Append('#');
                    break;
                case IcuPluralNode plural:
                    sb.Append('{');
                    sb.Append(plural.Variable);
                    sb.Append(", plural,");
                    foreach (var branch in plural.Branches)
                    {
                        sb.Append(' ');
                        sb.Append(branch.Category);
                        sb.Append(" {");
                        TransformNodes(branch.Content, sb);
                        sb.Append('}');
                    }
                    sb.Append('}');
                    break;
            }
        }
    }

    private static void AccentText(string text, StringBuilder sb)
    {
        var i = 0;
        while (i < text.Length)
        {
            // Check for <N> or </N> placeholder markers
            if (text[i] == '<')
            {
                var end = text.IndexOf('>', i + 1);
                if (end > i)
                {
                    var tag = text.Substring(i, end - i + 1);
                    if (IsPlaceholderTag(tag))
                    {
                        sb.Append(tag);
                        i = end + 1;
                        continue;
                    }
                }
            }

            if (char.IsLetter(text[i]))
                sb.Append(Accent(text[i]));
            else
                sb.Append(text[i]);

            i++;
        }
    }

    private static bool IsPlaceholderTag(string tag)
    {
        // Match <N> or </N> where N is digits
        if (tag.Length < 3) return false;
        if (tag[0] != '<' || tag[tag.Length - 1] != '>') return false;

        var inner = tag.Substring(1, tag.Length - 2);
        if (inner.StartsWith("/"))
            inner = inner.Substring(1);

        foreach (var c in inner)
        {
            if (!char.IsDigit(c)) return false;
        }
        return inner.Length > 0;
    }

    private static char GetCombiningChar(char c)
    {
        var lower = char.ToLowerInvariant(c);
        switch (lower)
        {
            case 'a': case 'u':
                return RingAbove;
            case 'e': case 'i': case 'h': case 'o': case 'w': case 'x': case 'y':
                return Diaeresis;
            case 'b': case 'd': case 'f': case 'q':
                return DotAbove;
            case 'v':
                return Tilde;
            case 't':
                return Cedilla;
            default:
                return AcuteAccent;
        }
    }
}
