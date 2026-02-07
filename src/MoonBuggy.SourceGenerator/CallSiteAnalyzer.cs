using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MoonBuggy.SourceGenerator;

internal sealed class CallSiteInfo
{
    public string Message { get; set; } = "";
    public string? Context { get; set; }
    public bool IsMarkdown { get; set; }
    public string InterceptableLocationVersion { get; set; } = "";
    public string InterceptableLocationData { get; set; } = "";
    public List<(string Name, string TypeName)> ArgProperties { get; set; } = new List<(string, string)>();
    public Location Location { get; set; } = Location.None;
}

internal static class CallSiteAnalyzer
{
    public static (CallSiteInfo? Info, Diagnostic? Error) Analyze(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        // Check if this is a _t or _m call
        string? methodName = null;
        if (invocation.Expression is IdentifierNameSyntax id)
        {
            methodName = id.Identifier.Text;
        }
        else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            methodName = memberAccess.Name.Identifier.Text;
        }

        if (methodName != "_t" && methodName != "_m")
            return (null, null);

        // Verify it resolves to MoonBuggy.Translate._t/_m
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return (null, null);

        if (methodSymbol.ContainingType?.ToDisplayString() != "MoonBuggy.Translate")
            return (null, null);

        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
            return (null, null);

        // First argument: message (must be constant)
        var messageArg = args[0].Expression;
        var messageConstant = semanticModel.GetConstantValue(messageArg);
        if (!messageConstant.HasValue || messageConstant.Value is not string messageStr)
        {
            return (null, Diagnostic.Create(
                Diagnostics.NonConstantMessage,
                messageArg.GetLocation()));
        }

        if (string.IsNullOrEmpty(messageStr))
        {
            return (null, Diagnostic.Create(
                Diagnostics.EmptyMessage,
                messageArg.GetLocation()));
        }

        // Extract context (named argument)
        string? context = null;
        foreach (var arg in args)
        {
            if (arg.NameColon?.Name.Identifier.Text == "context")
            {
                var contextConstant = semanticModel.GetConstantValue(arg.Expression);
                if (!contextConstant.HasValue || (contextConstant.Value != null && contextConstant.Value is not string))
                {
                    return (null, Diagnostic.Create(
                        Diagnostics.NonConstantContext,
                        arg.Expression.GetLocation()));
                }
                context = contextConstant.Value as string;
            }
        }

        // Extract args properties from anonymous type
        var argProperties = new List<(string Name, string TypeName)>();
        foreach (var arg in args)
        {
            // Skip named context arg and first message arg
            if (arg.NameColon?.Name.Identifier.Text == "context")
                continue;

            if (arg == args[0])
                continue;

            // Skip named context: argument
            if (arg.NameColon != null)
                continue;

            var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
            if (typeInfo.Type is INamedTypeSymbol namedType && namedType.IsAnonymousType)
            {
                foreach (var member in namedType.GetMembers().OfType<IPropertySymbol>())
                {
                    argProperties.Add((member.Name, member.Type.ToDisplayString()));
                }
            }
        }

        // Get interceptable location
        var interceptableLocation = semanticModel.GetInterceptableLocation(invocation);
        if (interceptableLocation == null)
            return (null, null);

        return (new CallSiteInfo
        {
            Message = messageStr,
            Context = context,
            IsMarkdown = methodName == "_m",
            InterceptableLocationVersion = interceptableLocation.Version.ToString(),
            InterceptableLocationData = interceptableLocation.Data,
            ArgProperties = argProperties,
            Location = invocation.GetLocation()
        }, null);
    }
}
