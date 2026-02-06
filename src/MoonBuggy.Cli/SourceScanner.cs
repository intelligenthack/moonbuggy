using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace MoonBuggy.Cli;

public record ExtractedMessage(
    string MbSyntax, string? Context, string FilePath, int LineNumber);

public static class SourceScanner
{
    // Match _t( but not _tm( or other prefixed variants — require non-word char or start before _t
    private static readonly Regex CallPattern = new Regex(
        @"(?<!\w)_t\s*\(",
        RegexOptions.Compiled);

    public static IReadOnlyList<ExtractedMessage> ScanText(string sourceText, string filePath)
    {
        var results = new List<ExtractedMessage>();
        var lines = sourceText.Split('\n');

        // Build a line-start offset table
        var lineOffsets = new int[lines.Length];
        var offset = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            lineOffsets[i] = offset;
            offset += lines[i].Length + 1; // +1 for \n
        }

        foreach (Match match in CallPattern.Matches(sourceText))
        {
            var parenStart = match.Index + match.Length - 1; // position of '('
            var lineNumber = GetLineNumber(lineOffsets, match.Index);

            var argContent = ExtractArgList(sourceText, parenStart);
            if (argContent == null)
                continue;

            var parsed = ParseArgList(sourceText, parenStart + 1, argContent.Length);
            if (parsed == null)
                continue;

            results.Add(new ExtractedMessage(
                parsed.Value.Message, parsed.Value.Context, filePath, lineNumber));
        }

        return results;
    }

    public static IReadOnlyList<ExtractedMessage> ScanFile(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return ScanText(text, filePath);
    }

    public static IReadOnlyList<ExtractedMessage> ScanFiles(
        string baseDir, IReadOnlyList<string> includePatterns)
    {
        var matcher = new Matcher();
        foreach (var pattern in includePatterns)
            matcher.AddInclude(pattern);

        var dirInfo = new DirectoryInfoWrapper(new DirectoryInfo(baseDir));
        var matchResult = matcher.Execute(dirInfo);

        var results = new List<ExtractedMessage>();
        foreach (var file in matchResult.Files)
        {
            var fullPath = Path.Combine(baseDir, file.Path);
            results.AddRange(ScanFile(fullPath));
        }

        return results;
    }

    private static int GetLineNumber(int[] lineOffsets, int charOffset)
    {
        // Binary search for the line containing charOffset
        var lo = 0;
        var hi = lineOffsets.Length - 1;
        while (lo < hi)
        {
            var mid = lo + (hi - lo + 1) / 2;
            if (lineOffsets[mid] <= charOffset)
                lo = mid;
            else
                hi = mid - 1;
        }
        return lo + 1; // 1-based
    }

    /// <summary>
    /// Extracts the content between matched parens starting at parenStart.
    /// Returns the content between '(' and ')' or null if unbalanced.
    /// </summary>
    private static string? ExtractArgList(string source, int parenStart)
    {
        var depth = 0;
        var i = parenStart;
        while (i < source.Length)
        {
            var c = source[i];
            if (c == '(')
                depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(parenStart + 1, i - parenStart - 1);
            }
            else if (c == '"')
            {
                // skip string literal
                if (i > 0 && source[i - 1] == '@')
                    i = SkipVerbatimString(source, i);
                else
                    i = SkipRegularString(source, i);
            }

            i++;
        }
        return null;
    }

    private struct ParsedArgs
    {
        public string Message;
        public string? Context;
    }

    /// <summary>
    /// Parse the argument list to extract the first string literal (message)
    /// and optional context (3rd arg or named "context:" arg).
    /// </summary>
    private static ParsedArgs? ParseArgList(string source, int contentStart, int contentLength)
    {
        var pos = contentStart;
        var end = contentStart + contentLength;

        // Skip whitespace
        pos = SkipWhitespace(source, pos, end);
        if (pos >= end)
            return null;

        // First arg must be a string literal
        var firstStr = TryParseStringLiteral(source, pos, end);
        if (firstStr == null)
            return null;

        var message = firstStr.Value.Value;
        pos = firstStr.Value.EndPos;

        // Now look for context
        string? context = null;

        // Find remaining arguments
        pos = SkipWhitespace(source, pos, end);
        if (pos < end && source[pos] == ',')
        {
            pos++; // skip comma
            pos = SkipWhitespace(source, pos, end);

            // Check for named "context:" argument
            if (pos < end && LookingAt(source, pos, "context:"))
            {
                pos += "context:".Length;
                pos = SkipWhitespace(source, pos, end);
                var ctxStr = TryParseStringLiteral(source, pos, end);
                if (ctxStr != null)
                    context = ctxStr.Value.Value;
            }
            else
            {
                // Positional: 2nd arg (skip it), then look for 3rd
                var secondArgEnd = SkipArgument(source, pos, end);
                pos = secondArgEnd;
                pos = SkipWhitespace(source, pos, end);

                if (pos < end && source[pos] == ',')
                {
                    pos++; // skip comma
                    pos = SkipWhitespace(source, pos, end);

                    // Check for named "context:" before the string
                    if (pos < end && LookingAt(source, pos, "context:"))
                    {
                        pos += "context:".Length;
                        pos = SkipWhitespace(source, pos, end);
                    }

                    var thirdStr = TryParseStringLiteral(source, pos, end);
                    if (thirdStr != null)
                        context = thirdStr.Value.Value;
                }
            }
        }

        return new ParsedArgs { Message = message, Context = context };
    }

    private static bool LookingAt(string source, int pos, string text)
    {
        if (pos + text.Length > source.Length)
            return false;
        for (var i = 0; i < text.Length; i++)
        {
            if (source[pos + i] != text[i])
                return false;
        }
        return true;
    }

    private struct StringLiteralResult
    {
        public string Value;
        public int EndPos;
    }

    private static StringLiteralResult? TryParseStringLiteral(string source, int pos, int end)
    {
        if (pos >= end)
            return null;

        // Verbatim string
        if (source[pos] == '@' && pos + 1 < end && source[pos + 1] == '"')
        {
            var startContent = pos + 2;
            var i = startContent;
            var value = new System.Text.StringBuilder();
            while (i < source.Length)
            {
                if (source[i] == '"')
                {
                    if (i + 1 < source.Length && source[i + 1] == '"')
                    {
                        value.Append('"');
                        i += 2;
                    }
                    else
                    {
                        return new StringLiteralResult { Value = value.ToString(), EndPos = i + 1 };
                    }
                }
                else
                {
                    value.Append(source[i]);
                    i++;
                }
            }
            return null;
        }

        // Regular string
        if (source[pos] == '"')
        {
            var startContent = pos + 1;
            var i = startContent;
            var value = new System.Text.StringBuilder();
            while (i < source.Length)
            {
                if (source[i] == '\\' && i + 1 < source.Length)
                {
                    switch (source[i + 1])
                    {
                        case '"': value.Append('"'); break;
                        case '\\': value.Append('\\'); break;
                        case 'n': value.Append('\n'); break;
                        case 't': value.Append('\t'); break;
                        case 'r': value.Append('\r'); break;
                        default: value.Append('\\'); value.Append(source[i + 1]); break;
                    }
                    i += 2;
                }
                else if (source[i] == '"')
                {
                    return new StringLiteralResult { Value = value.ToString(), EndPos = i + 1 };
                }
                else
                {
                    value.Append(source[i]);
                    i++;
                }
            }
            return null;
        }

        return null;
    }

    /// <summary>
    /// Skip a single argument expression (handles nested parens, strings, etc.)
    /// Returns position after the argument (at comma, closing paren, or end).
    /// </summary>
    private static int SkipArgument(string source, int pos, int end)
    {
        var depth = 0;
        while (pos < end)
        {
            var c = source[pos];
            if (c == ',' && depth == 0)
                return pos;
            if (c == '(' || c == '{' || c == '[')
                depth++;
            else if (c == ')' || c == '}' || c == ']')
            {
                if (depth == 0)
                    return pos;
                depth--;
            }
            else if (c == '"')
            {
                if (pos > 0 && source[pos - 1] == '@')
                    pos = SkipVerbatimString(source, pos);
                else
                    pos = SkipRegularString(source, pos);
            }
            pos++;
        }
        return pos;
    }

    /// <summary>
    /// Given position at opening '"' of a regular string, return position of closing '"'.
    /// </summary>
    private static int SkipRegularString(string source, int pos)
    {
        pos++; // skip opening "
        while (pos < source.Length)
        {
            if (source[pos] == '\\')
                pos++; // skip escaped char
            else if (source[pos] == '"')
                return pos;
            pos++;
        }
        return pos;
    }

    /// <summary>
    /// Given position at opening '"' of @"..." verbatim string, return position of closing '"'.
    /// </summary>
    private static int SkipVerbatimString(string source, int pos)
    {
        pos++; // skip opening "
        while (pos < source.Length)
        {
            if (source[pos] == '"')
            {
                if (pos + 1 < source.Length && source[pos + 1] == '"')
                    pos++; // skip doubled quote
                else
                    return pos;
            }
            pos++;
        }
        return pos;
    }

    private static int SkipWhitespace(string source, int pos, int end)
    {
        while (pos < end && char.IsWhiteSpace(source[pos]))
            pos++;
        return pos;
    }
}
