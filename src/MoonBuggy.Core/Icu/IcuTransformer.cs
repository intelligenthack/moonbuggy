using System.Text;
using MoonBuggy.Core.Parsing;

namespace MoonBuggy.Core.Icu;

public static class IcuTransformer
{
    public static string Transform(MbToken[] tokens)
    {
        var sb = new StringBuilder();
        foreach (var token in tokens)
            AppendToken(sb, token);
        return sb.ToString();
    }

    public static string ToIcu(string mbSyntax)
    {
        var tokens = MbParser.Parse(mbSyntax);
        return Transform(tokens);
    }

    private static void AppendToken(StringBuilder sb, MbToken token)
    {
        switch (token)
        {
            case TextToken t:
                sb.Append(t.Value);
                break;

            case VariableToken v:
                sb.Append('{');
                sb.Append(v.Name);
                sb.Append('}');
                break;

            case PluralSelectorRef:
                sb.Append('#');
                break;

            case PluralBlockToken p:
                sb.Append('{');
                sb.Append(p.SelectorVariable);
                sb.Append(", plural,");
                foreach (var form in p.Forms)
                {
                    sb.Append(' ');
                    sb.Append(form.Category);
                    sb.Append(" {");
                    foreach (var ft in form.Content)
                        AppendToken(sb, ft);
                    sb.Append('}');
                }
                sb.Append('}');
                break;
        }
    }
}
