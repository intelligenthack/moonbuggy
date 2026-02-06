using System.Text;

namespace MoonBuggy.Core.Parsing;

public static class MbParser
{
    public static MbToken[] Parse(string input)
    {
        if (input.Length == 0)
            throw new FormatException("Empty message string.");

        var tokens = new List<MbToken>();
        var pos = 0;
        var text = new StringBuilder();

        while (pos < input.Length)
        {
            var ch = input[pos];

            if (ch == '$')
            {
                // Flush accumulated text
                if (text.Length > 0)
                {
                    tokens.Add(new TextToken(text.ToString()));
                    text.Clear();
                }

                pos++;
                if (pos < input.Length && input[pos] == '$')
                {
                    // $$ → literal $
                    text.Append('$');
                    pos++;
                }
                else
                {
                    // Start of $...$ block — variable or plural
                    pos = ParseDollarBlock(input, pos, tokens);
                }
            }
            else
            {
                text.Append(ch);
                pos++;
            }
        }

        if (text.Length > 0)
            tokens.Add(new TextToken(text.ToString()));

        return tokens.ToArray();
    }

    private static int ParseDollarBlock(string input, int pos, List<MbToken> tokens)
    {
        // First, check if this is a plural block by pre-scanning for an unescaped pipe.
        // This is needed because plural blocks can contain nested $var$ patterns.
        var isPlural = PreScanForPipe(input, pos);

        var content = new StringBuilder();
        var scanPos = pos;

        while (scanPos < input.Length)
        {
            var ch = input[scanPos];
            if (ch == '$')
            {
                if (scanPos + 1 < input.Length && input[scanPos + 1] == '$')
                {
                    // $$ escape inside block
                    content.Append("$$");
                    scanPos += 2;
                    continue;
                }

                // In a plural block, $var$ is a nested variable, not the block closer
                if (isPlural && IsNestedVariable(input, scanPos))
                {
                    content.Append(ch);
                    scanPos++;
                    while (scanPos < input.Length && input[scanPos] != '$')
                    {
                        content.Append(input[scanPos]);
                        scanPos++;
                    }
                    content.Append('$');
                    scanPos++;
                    continue;
                }

                break; // closing $ of the block
            }
            content.Append(ch);
            scanPos++;
        }

        if (scanPos >= input.Length)
            throw new FormatException($"Unmatched '$' at position {pos - 1}.");

        var blockContent = content.ToString();
        scanPos++; // skip closing $

        if (isPlural)
        {
            tokens.Add(ParsePluralBlock(blockContent));
        }
        else
        {
            tokens.Add(new VariableToken(blockContent));
        }

        return scanPos;
    }

    private static bool PreScanForPipe(string input, int pos)
    {
        // Scan forward from pos to find an unescaped | before the closing $
        // This handles $$ escapes and nested $var$ correctly
        var i = pos;
        while (i < input.Length)
        {
            var ch = input[i];
            if (ch == '$')
            {
                if (i + 1 < input.Length && input[i + 1] == '$')
                {
                    i += 2; // skip $$
                    continue;
                }
                // This could be a nested $var$ or the block closer
                if (IsNestedVariable(input, i))
                {
                    // Skip past the nested $var$
                    i++; // skip opening $
                    while (i < input.Length && input[i] != '$') i++;
                    i++; // skip closing $
                    continue;
                }
                return false; // reached closing $ without finding pipe
            }
            if (ch == '|')
            {
                if (i + 1 < input.Length && input[i + 1] == '|')
                {
                    i += 2; // skip ||
                    continue;
                }
                return true; // found unescaped pipe
            }
            i++;
        }
        return false;
    }

    private static bool IsNestedVariable(string input, int dollarPos)
    {
        // Check if $...$ at dollarPos looks like a variable: $identifier$
        // i.e., $ followed by one or more identifier chars, then $
        var i = dollarPos + 1;
        if (i >= input.Length || !IsIdentChar(input[i]))
            return false;

        while (i < input.Length && IsIdentChar(input[i]))
            i++;

        return i < input.Length && input[i] == '$';
    }

    private static bool IsIdentChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static PluralBlockToken ParsePluralBlock(string blockContent)
    {
        // Split on unescaped pipes
        var forms = SplitOnPipes(blockContent);

        // Find the selector from the first #var# or #~var# or #var=0# occurrence
        string selectorVar = "";
        var rendered = true;
        var hasZero = false;

        // Scan the entire block for the first selector
        var selectorFound = false;
        for (var i = 0; i < blockContent.Length && !selectorFound; i++)
        {
            if (blockContent[i] == '#' && !(i + 1 < blockContent.Length && blockContent[i + 1] == '#'))
            {
                i++; // skip opening #
                var hidden = false;
                if (i < blockContent.Length && blockContent[i] == '~')
                {
                    hidden = true;
                    i++;
                }

                var name = new StringBuilder();
                while (i < blockContent.Length && blockContent[i] != '#')
                {
                    name.Append(blockContent[i]);
                    i++;
                }

                var nameStr = name.ToString();
                if (nameStr.EndsWith("=0"))
                {
                    hasZero = true;
                    nameStr = nameStr.Substring(0, nameStr.Length - 2);
                }

                selectorVar = nameStr;
                rendered = !hidden;
                selectorFound = true;
            }
        }

        // Build form categories
        string[] categories;
        if (hasZero)
        {
            if (forms.Count != 3)
                throw new FormatException($"Plural block with =0 requires exactly 3 forms, got {forms.Count}.");
            categories = new[] { "=0", "one", "other" };
        }
        else
        {
            if (forms.Count != 2)
                throw new FormatException($"Plural block without =0 requires exactly 2 forms, got {forms.Count}.");
            categories = new[] { "one", "other" };
        }

        var pluralForms = new PluralForm[forms.Count];
        var isFirstForm = true;
        for (var i = 0; i < forms.Count; i++)
        {
            var formTokens = ParsePluralFormContent(forms[i], selectorVar, isFirstForm);
            pluralForms[i] = new PluralForm(categories[i], formTokens);
            isFirstForm = false;
        }

        return new PluralBlockToken(selectorVar, rendered, hasZero, pluralForms);
    }

    private static MbToken[] ParsePluralFormContent(string content, string selectorVar, bool isFirstForm)
    {
        var tokens = new List<MbToken>();
        var text = new StringBuilder();
        var pos = 0;
        var firstHashSeen = false;

        while (pos < content.Length)
        {
            var ch = content[pos];

            if (ch == '#')
            {
                if (pos + 1 < content.Length && content[pos + 1] == '#')
                {
                    // ## → literal #
                    text.Append('#');
                    pos += 2;
                    continue;
                }

                // Flush text
                if (text.Length > 0)
                {
                    tokens.Add(new TextToken(text.ToString()));
                    text.Clear();
                }

                pos++; // skip opening #
                var hidden = false;
                if (pos < content.Length && content[pos] == '~')
                {
                    hidden = true;
                    pos++;
                }

                var name = new StringBuilder();
                while (pos < content.Length && content[pos] != '#')
                {
                    name.Append(content[pos]);
                    pos++;
                }
                pos++; // skip closing #

                var nameStr = name.ToString();
                var isSelectorDeclaration = false;
                if (nameStr.EndsWith("=0"))
                {
                    nameStr = nameStr.Substring(0, nameStr.Length - 2);
                    // #var=0# in the first form is the selector declaration — don't render
                    if (isFirstForm && !firstHashSeen)
                        isSelectorDeclaration = true;
                }

                firstHashSeen = true;

                if (!hidden && !isSelectorDeclaration)
                {
                    // Rendered selector reference — represents the ICU # placeholder
                    tokens.Add(new PluralSelectorRef(nameStr));
                }
                // If hidden or selector declaration, emit nothing
            }
            else if (ch == '$')
            {
                if (pos + 1 < content.Length && content[pos + 1] == '$')
                {
                    text.Append('$');
                    pos += 2;
                    continue;
                }

                // Flush text
                if (text.Length > 0)
                {
                    tokens.Add(new TextToken(text.ToString()));
                    text.Clear();
                }

                pos++; // skip opening $
                var varName = new StringBuilder();
                while (pos < content.Length && content[pos] != '$')
                {
                    varName.Append(content[pos]);
                    pos++;
                }
                pos++; // skip closing $

                tokens.Add(new VariableToken(varName.ToString()));
            }
            else if (ch == '|' && pos + 1 < content.Length && content[pos + 1] == '|')
            {
                // || → literal |
                text.Append('|');
                pos += 2;
            }
            else
            {
                text.Append(ch);
                pos++;
            }
        }

        if (text.Length > 0)
            tokens.Add(new TextToken(text.ToString()));

        return tokens.ToArray();
    }

    private static List<string> SplitOnPipes(string content)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var i = 0;

        while (i < content.Length)
        {
            if (content[i] == '|')
            {
                if (i + 1 < content.Length && content[i + 1] == '|')
                {
                    // || → escaped pipe, keep as ||  for later processing
                    current.Append("||");
                    i += 2;
                }
                else
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    i++;
                }
            }
            else
            {
                current.Append(content[i]);
                i++;
            }
        }

        parts.Add(current.ToString());
        return parts;
    }
}
