using System.Collections.Generic;
using System.Text;

namespace MoonBuggy.Core.Icu;

public static class IcuParser
{
    public static IcuNode[] Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new IcuNode[0];

        var nodes = new List<IcuNode>();
        var pos = 0;
        ParseSequence(input, nodes, ref pos, false);
        return nodes.ToArray();
    }

    private static void ParseSequence(string input, List<IcuNode> nodes, ref int pos, bool insidePlural)
    {
        var text = new StringBuilder();

        while (pos < input.Length)
        {
            var ch = input[pos];

            if (ch == '#' && insidePlural)
            {
                FlushText(text, nodes);
                nodes.Add(new IcuHashNode());
                pos++;
            }
            else if (ch == '{')
            {
                FlushText(text, nodes);
                pos++; // skip '{'
                var name = ReadUntil(input, ref pos, '}', ',');

                if (pos < input.Length && input[pos] == ',')
                {
                    // Could be {var, plural, ...}
                    pos++; // skip ','
                    var keyword = ReadUntil(input, ref pos, ',', '}').Trim();

                    if (keyword == "plural" && pos < input.Length && input[pos] == ',')
                    {
                        pos++; // skip ','
                        var branches = ParsePluralBranches(input, ref pos);
                        nodes.Add(new IcuPluralNode(name, branches));
                        // pos should be past the closing '}'
                    }
                    else
                    {
                        // Unknown format — treat as variable
                        nodes.Add(new IcuVariableNode(name));
                        // Skip to closing '}'
                        SkipToClosingBrace(input, ref pos);
                    }
                }
                else if (pos < input.Length && input[pos] == '}')
                {
                    // Simple variable: {name}
                    nodes.Add(new IcuVariableNode(name));
                    pos++; // skip '}'
                }
            }
            else if (ch == '}' && insidePlural)
            {
                // End of plural branch content — don't consume
                break;
            }
            else if (ch == '\'' )
            {
                // ICU escaping: '' → literal ', '{ → literal {, etc.
                pos++;
                if (pos < input.Length && input[pos] == '\'')
                {
                    // '' → literal '
                    text.Append('\'');
                    pos++;
                }
                else
                {
                    // Quoted sequence until next unescaped '
                    while (pos < input.Length)
                    {
                        if (input[pos] == '\'' )
                        {
                            pos++;
                            // '' inside quoted = literal '
                            if (pos < input.Length && input[pos] == '\'')
                            {
                                text.Append('\'');
                                pos++;
                            }
                            else
                            {
                                break; // end of quoted sequence
                            }
                        }
                        else
                        {
                            text.Append(input[pos]);
                            pos++;
                        }
                    }
                }
            }
            else
            {
                text.Append(ch);
                pos++;
            }
        }

        FlushText(text, nodes);
    }

    private static IcuPluralBranch[] ParsePluralBranches(string input, ref int pos)
    {
        var branches = new List<IcuPluralBranch>();

        SkipWhitespace(input, ref pos);

        while (pos < input.Length && input[pos] != '}')
        {
            // Read category (e.g., "one", "other", "=0")
            var category = ReadUntil(input, ref pos, '{', '}').Trim();

            if (pos < input.Length && input[pos] == '{')
            {
                pos++; // skip '{'

                // Parse branch content
                var content = new List<IcuNode>();
                ParseSequence(input, content, ref pos, true);

                if (pos < input.Length && input[pos] == '}')
                    pos++; // skip '}'

                branches.Add(new IcuPluralBranch(category, content.ToArray()));
            }

            SkipWhitespace(input, ref pos);
        }

        if (pos < input.Length && input[pos] == '}')
            pos++; // skip outer closing '}'

        return branches.ToArray();
    }

    private static string ReadUntil(string input, ref int pos, char stop1, char stop2)
    {
        var sb = new StringBuilder();
        while (pos < input.Length && input[pos] != stop1 && input[pos] != stop2)
        {
            sb.Append(input[pos]);
            pos++;
        }
        return sb.ToString().Trim();
    }

    private static void SkipWhitespace(string input, ref int pos)
    {
        while (pos < input.Length && char.IsWhiteSpace(input[pos]))
            pos++;
    }

    private static void SkipToClosingBrace(string input, ref int pos)
    {
        var depth = 1;
        while (pos < input.Length && depth > 0)
        {
            if (input[pos] == '{') depth++;
            else if (input[pos] == '}') depth--;
            pos++;
        }
    }

    private static void FlushText(StringBuilder text, List<IcuNode> nodes)
    {
        if (text.Length > 0)
        {
            nodes.Add(new IcuTextNode(text.ToString()));
            text.Clear();
        }
    }
}
