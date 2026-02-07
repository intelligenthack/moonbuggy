using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MoonBuggy.Core.Parsing;
using MoonBuggy.Core.Po;

namespace MoonBuggy.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class MoonBuggyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all invocation expressions that might be _t or _m calls
        var callSites = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsTranslateInvocation(node),
                transform: static (ctx, ct) => AnalyzeCallSite(ctx, ct))
            .Where(static result => result != null)
            .Collect();

        // Collect PO files from AdditionalTexts
        var poFiles = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, ct) => ParsePoFile(text, ct))
            .Where(static result => result != null)
            .Collect();

        // Read MoonBuggyPseudoLocale MSBuild property
        var pseudoLocale = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.MoonBuggyPseudoLocale", out var value);
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            });

        // Combine call sites with PO files and pseudo-locale flag to emit interceptors
        var combined = callSites.Combine(poFiles).Combine(pseudoLocale);

        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var ((callSiteResults, poFileResults), pseudoLocaleEnabled) = source;
            Execute(spc, callSiteResults!, poFileResults!, pseudoLocaleEnabled);
        });
    }

    private static bool IsTranslateInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        var name = invocation.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
            _ => null
        };

        return name == "_t" || name == "_m";
    }

    private static CallSiteResult? AnalyzeCallSite(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;
        var (info, error) = CallSiteAnalyzer.Analyze(invocation, ctx.SemanticModel);

        if (info == null && error == null)
            return null;

        return new CallSiteResult(info, error);
    }

    private static LocaleTranslation? ParsePoFile(AdditionalText text, CancellationToken ct)
    {
        var content = text.GetText(ct)?.ToString();
        if (string.IsNullOrEmpty(content))
            return null;

        // Extract locale from path: locales/<locale>/messages.po
        var locale = ExtractLocaleFromPath(text.Path);
        if (locale == null)
            return null;

        try
        {
            var catalog = PoReader.Read(content!);
            var lcid = GetLcid(locale);
            if (lcid == 0)
                return null;

            return new LocaleTranslation
            {
                Locale = locale,
                Lcid = lcid,
                Catalog = catalog
            };
        }
        catch
        {
            return null;
        }
    }

    internal static string? ExtractLocaleFromPath(string path)
    {
        // Normalize separators
        var normalized = path.Replace('\\', '/');

        // Look for patterns like locales/<locale>/something.po
        // or <locale>/messages.po
        var parts = normalized.Split('/');
        for (int i = parts.Length - 2; i >= 0; i--)
        {
            var candidate = parts[i];

            // Skip "locales" folder name
            if (string.Equals(candidate, "locales", StringComparison.OrdinalIgnoreCase))
                continue;

            // Try to validate as a locale
            if (IsValidLocale(candidate))
                return candidate;
        }

        return null;
    }

    private static bool IsValidLocale(string locale)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(locale);
            return culture.LCID != 4096; // 4096 = InvariantCulture or unknown
        }
        catch
        {
            return false;
        }
    }

    internal static int GetLcid(string locale)
    {
        try
        {
            return CultureInfo.GetCultureInfo(locale).LCID;
        }
        catch
        {
            return 0;
        }
    }

    private static void Execute(
        SourceProductionContext spc,
        ImmutableArray<CallSiteResult?> callSiteResults,
        ImmutableArray<LocaleTranslation?> poFileResults,
        bool pseudoLocaleEnabled)
    {
        var callSites = new List<CallSiteInfo>();
        var diagnosticsList = new List<Diagnostic>();

        foreach (var result in callSiteResults)
        {
            if (result == null) continue;

            if (result.Error != null)
            {
                spc.ReportDiagnostic(result.Error);
                continue;
            }

            if (result.Info != null)
            {
                // Validate MB syntax
                MbToken[] tokens;
                try
                {
                    tokens = MbParser.Parse(result.Info.Message);
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.MalformedMbSyntax,
                        result.Info.Location,
                        ex.Message));
                    continue;
                }

                // Validate that all MB variables have matching arg properties
                var varNames = CollectVariableNames(tokens);
                var argNames = new HashSet<string>(result.Info.ArgProperties.Select(a => a.Name));
                var hasMissing = false;
                foreach (var v in varNames)
                {
                    if (!argNames.Contains(v))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.MissingArgProperty,
                            result.Info.Location,
                            v));
                        hasMissing = true;
                    }
                }
                if (hasMissing)
                    continue;

                callSites.Add(result.Info);
            }
        }

        if (callSites.Count == 0)
            return;

        var translations = poFileResults.Where(t => t != null).Select(t => t!).ToList();

        var source = InterceptorEmitter.Emit(callSites, translations, pseudoLocaleEnabled);
        spc.AddSource("MoonBuggy.Interceptors.g.cs", SourceText.From(source, Encoding.UTF8));
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
}

internal sealed class CallSiteResult
{
    public CallSiteInfo? Info { get; }
    public Diagnostic? Error { get; }

    public CallSiteResult(CallSiteInfo? info, Diagnostic? error)
    {
        Info = info;
        Error = error;
    }
}
