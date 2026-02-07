using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MoonBuggy.Core.Parsing;

namespace MoonBuggy.SourceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MoonBuggyAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics =
        ImmutableArray.Create(
            Diagnostics.NonConstantMessage,
            Diagnostics.MissingArgProperty,
            Diagnostics.ExtraArgProperty,
            Diagnostics.MalformedMbSyntax,
            Diagnostics.EmptyMessage,
            Diagnostics.NonConstantContext,
            Diagnostics.PluralSelectorNotInteger);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Fast syntax check: is this _t or _m?
        string? methodName = invocation.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
            _ => null
        };

        if (methodName != "_t" && methodName != "_m")
            return;

        // Verify it resolves to MoonBuggy.Translate._t/_m
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (methodSymbol.ContainingType?.ToDisplayString() != "MoonBuggy.Translate")
            return;

        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
            return;

        // Check first argument: must be constant string
        var messageArg = args[0].Expression;
        var messageConstant = context.SemanticModel.GetConstantValue(messageArg, context.CancellationToken);
        if (!messageConstant.HasValue || messageConstant.Value is not string messageStr)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.NonConstantMessage, messageArg.GetLocation()));
            return;
        }

        // Check empty message
        if (string.IsNullOrEmpty(messageStr))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.EmptyMessage, messageArg.GetLocation()));
            return;
        }

        // Check context argument
        foreach (var arg in args)
        {
            if (arg.NameColon?.Name.Identifier.Text == "context")
            {
                var contextConstant = context.SemanticModel.GetConstantValue(arg.Expression, context.CancellationToken);
                if (!contextConstant.HasValue || (contextConstant.Value != null && contextConstant.Value is not string))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.NonConstantContext, arg.Expression.GetLocation()));
                    return;
                }
            }
        }

        // Parse MB syntax
        MbToken[] tokens;
        try
        {
            tokens = MbParser.Parse(messageStr);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MalformedMbSyntax, messageArg.GetLocation(), ex.Message));
            return;
        }

        // Collect variable names from MB tokens
        var varNames = CollectVariableNames(tokens);

        // Collect plural selector variable names
        var pluralSelectorNames = CollectPluralSelectorNames(tokens);

        // Extract arg properties from anonymous type
        var argProperties = new Dictionary<string, ITypeSymbol>();
        foreach (var arg in args)
        {
            if (arg.NameColon?.Name.Identifier.Text == "context")
                continue;
            if (arg == args[0])
                continue;
            if (arg.NameColon != null)
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken);
            if (typeInfo.Type is INamedTypeSymbol namedType && namedType.IsAnonymousType)
            {
                foreach (var member in namedType.GetMembers().OfType<IPropertySymbol>())
                {
                    argProperties[member.Name] = member.Type;
                }
            }
        }

        // Check MB0002: missing arg properties
        foreach (var v in varNames)
        {
            if (!argProperties.ContainsKey(v))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MissingArgProperty, invocation.GetLocation(), v));
            }
        }

        // Check MB0003: extra arg properties
        foreach (var prop in argProperties)
        {
            if (!varNames.Contains(prop.Key))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ExtraArgProperty, invocation.GetLocation(), prop.Key));
            }
        }

        // Check MB0009: plural selector must be integer type
        foreach (var selectorName in pluralSelectorNames)
        {
            if (argProperties.TryGetValue(selectorName, out var selectorType))
            {
                if (!IsIntegerType(selectorType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.PluralSelectorNotInteger, invocation.GetLocation(), selectorName));
                }
            }
        }
    }

    private static HashSet<string> CollectVariableNames(MbToken[] tokens)
    {
        var names = new HashSet<string>();
        foreach (var token in tokens)
        {
            switch (token)
            {
                case VariableToken v:
                    names.Add(v.Name);
                    break;
                case PluralBlockToken p:
                    names.Add(p.SelectorVariable);
                    foreach (var form in p.Forms)
                        foreach (var ft in form.Content)
                            if (ft is VariableToken fv)
                                names.Add(fv.Name);
                    break;
            }
        }
        return names;
    }

    private static HashSet<string> CollectPluralSelectorNames(MbToken[] tokens)
    {
        var names = new HashSet<string>();
        foreach (var token in tokens)
        {
            if (token is PluralBlockToken p)
                names.Add(p.SelectorVariable);
        }
        return names;
    }

    private static bool IsIntegerType(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
                return true;
            default:
                return false;
        }
    }
}
