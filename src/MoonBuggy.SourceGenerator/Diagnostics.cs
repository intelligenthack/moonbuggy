using Microsoft.CodeAnalysis;

namespace MoonBuggy.SourceGenerator;

internal static class Diagnostics
{
    private const string Category = "MoonBuggy";

    public static readonly DiagnosticDescriptor NonConstantMessage = new(
        "MB0001",
        "Non-constant message",
        "The first argument to _t/_m must be a compile-time constant string",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingArgProperty = new(
        "MB0002",
        "Missing argument property",
        "Variable '{0}' in message has no matching property in args",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExtraArgProperty = new(
        "MB0003",
        "Extra argument property",
        "Property '{0}' in args is not used in the message",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PoFileNotFound = new(
        "MB0004",
        "PO file not found",
        "No PO file found for locale '{0}'",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MalformedMbSyntax = new(
        "MB0005",
        "Malformed MB syntax",
        "Failed to parse MB syntax: {0}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BadMarkdownOutput = new(
        "MB0006",
        "Bad markdown output",
        "Markdown processing produced unexpected output: {0}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EmptyMessage = new(
        "MB0007",
        "Empty message",
        "The message string must not be empty",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonConstantContext = new(
        "MB0008",
        "Non-constant context",
        "The context argument must be a compile-time constant string",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PluralSelectorNotInteger = new(
        "MB0009",
        "Plural selector is not an integer type",
        "Plural selector '{0}' must be an integer type (byte, short, int, long, etc.), not a floating-point or decimal type",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
