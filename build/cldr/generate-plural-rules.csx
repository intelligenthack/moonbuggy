#!/usr/bin/env dotnet-script
// Generates CldrPluralRules.generated.cs and CldrPluralCategories.generated.cs
// from CLDR plurals.json using MoonBuggy.Core's parser/simplifier/emitter.
//
// Usage: dotnet script build/cldr/generate-plural-rules.csx
//   (or: dotnet run --project build/cldr/GenerateCldr)

// Since dotnet-script can't easily reference a netstandard2.0 project,
// this script is a thin wrapper. The actual generation logic lives in
// MoonBuggy.Core.Plural.CldrPluralGenerator (used by both this script and tests).
//
// For simplicity, we use a self-contained implementation here that
// duplicates the core logic — but the canonical tested version is in Core.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

// ─── AST ────────────────────────────────────────────────────────────
class RangeValue { public long Low, High; public bool IsSingle => Low == High; public bool Contains(long v) => v >= Low && v <= High; }
class Relation { public string Operand; public int? Modulus; public bool Negated; public List<RangeValue> Ranges; }
class AndExpr { public List<Relation> Relations; }
class OrExpr { public List<AndExpr> Branches; }

// ─── Parser ─────────────────────────────────────────────────────────
static OrExpr ParseRule(string condition)
{
    int idx = condition.IndexOf('@');
    if (idx >= 0) condition = condition.Substring(0, idx);
    condition = condition.Trim();
    if (string.IsNullOrEmpty(condition)) return null;

    var tokens = Tokenize(condition);
    int pos = 0;
    return ParseOr(tokens, ref pos);
}

static List<string> Tokenize(string s)
{
    var t = new List<string>(); int i = 0;
    while (i < s.Length)
    {
        if (char.IsWhiteSpace(s[i])) { i++; continue; }
        if (s[i] == '!' && i+1 < s.Length && s[i+1] == '=') { t.Add("!="); i+=2; continue; }
        if (s[i] == '.' && i+1 < s.Length && s[i+1] == '.') { t.Add(".."); i+=2; continue; }
        if ("=%,".Contains(s[i])) { t.Add(s[i].ToString()); i++; continue; }
        int start = i;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
        if (i > start) t.Add(s.Substring(start, i - start));
    }
    return t;
}

static OrExpr ParseOr(List<string> t, ref int p)
{
    var branches = new List<AndExpr> { ParseAnd(t, ref p) };
    while (p < t.Count && t[p] == "or") { p++; branches.Add(ParseAnd(t, ref p)); }
    return new OrExpr { Branches = branches };
}

static AndExpr ParseAnd(List<string> t, ref int p)
{
    var rels = new List<Relation> { ParseRelation(t, ref p) };
    while (p < t.Count && t[p] == "and") { p++; rels.Add(ParseRelation(t, ref p)); }
    return new AndExpr { Relations = rels };
}

static Relation ParseRelation(List<string> t, ref int p)
{
    string op = t[p++];
    int? mod = null;
    if (p < t.Count && t[p] == "%") { p++; mod = int.Parse(t[p++]); }
    bool neg = false;
    if (p < t.Count && t[p] == "!=") { neg = true; p++; }
    else if (p < t.Count && t[p] == "=") { p++; }
    var ranges = new List<RangeValue> { ParseRange(t, ref p) };
    while (p < t.Count && t[p] == ",") { p++; ranges.Add(ParseRange(t, ref p)); }
    return new Relation { Operand = op, Modulus = mod, Negated = neg, Ranges = ranges };
}

static RangeValue ParseRange(List<string> t, ref int p)
{
    long low = long.Parse(t[p++]);
    if (p < t.Count && t[p] == "..") { p++; return new RangeValue { Low = low, High = long.Parse(t[p++]) }; }
    return new RangeValue { Low = low, High = low };
}

// ─── Integer Simplifier ─────────────────────────────────────────────
static bool IsZeroOp(string op) => op == "v" || op == "w" || op == "f" || op == "t" || op == "e";

static OrExpr Simplify(OrExpr expr)
{
    if (expr == null) return null;
    var live = new List<AndExpr>();
    foreach (var branch in expr.Branches)
    {
        var rels = new List<Relation>();
        bool dead = false;
        foreach (var rel in branch.Relations)
        {
            if (IsZeroOp(rel.Operand))
            {
                bool zeroMatches = rel.Ranges.Any(r => r.Contains(0));
                if (rel.Negated) { if (zeroMatches) { dead = true; break; } /* else drop */ }
                else { if (!zeroMatches) { dead = true; break; } /* else drop */ }
            }
            else
            {
                string newOp = rel.Operand == "i" ? "n" : rel.Operand;
                rels.Add(new Relation { Operand = newOp, Modulus = rel.Modulus, Negated = rel.Negated, Ranges = rel.Ranges });
            }
        }
        if (!dead) live.Add(new AndExpr { Relations = rels });
    }
    if (live.Count == 0) return null;
    return new OrExpr { Branches = live };
}

// ─── C# Emitter ─────────────────────────────────────────────────────
static string EmitCondition(OrExpr expr)
{
    if (expr == null) return null;
    var sb = new StringBuilder();
    for (int i = 0; i < expr.Branches.Count; i++)
    {
        if (i > 0) sb.Append(" || ");
        var branch = expr.Branches[i];
        if (branch.Relations.Count == 0) return null; // unconditional
        bool paren = expr.Branches.Count > 1 && branch.Relations.Count > 1;
        if (paren) sb.Append('(');
        for (int j = 0; j < branch.Relations.Count; j++)
        {
            if (j > 0) sb.Append(" && ");
            EmitRelation(sb, branch.Relations[j]);
        }
        if (paren) sb.Append(')');
    }
    return sb.ToString();
}

static void EmitRelation(StringBuilder sb, Relation rel)
{
    string e = rel.Modulus.HasValue ? $"n % {rel.Modulus.Value}" : "n";
    if (rel.Ranges.Count == 1 && rel.Ranges[0].IsSingle)
    {
        sb.Append($"{e} {(rel.Negated ? "!=" : "==")} {rel.Ranges[0].Low}");
    }
    else if (rel.Ranges.Count == 1 && !rel.Ranges[0].IsSingle)
    {
        if (rel.Negated) sb.Append($"({e} < {rel.Ranges[0].Low} || {e} > {rel.Ranges[0].High})");
        else sb.Append($"{e} >= {rel.Ranges[0].Low} && {e} <= {rel.Ranges[0].High}");
    }
    else
    {
        if (rel.Negated)
        {
            bool p = rel.Ranges.Count > 1; if (p) sb.Append('(');
            for (int i = 0; i < rel.Ranges.Count; i++)
            {
                if (i > 0) sb.Append(" && ");
                if (rel.Ranges[i].IsSingle) sb.Append($"{e} != {rel.Ranges[i].Low}");
                else sb.Append($"({e} < {rel.Ranges[i].Low} || {e} > {rel.Ranges[i].High})");
            }
            if (p) sb.Append(')');
        }
        else
        {
            bool p = rel.Ranges.Count > 1; if (p) sb.Append('(');
            for (int i = 0; i < rel.Ranges.Count; i++)
            {
                if (i > 0) sb.Append(" || ");
                if (rel.Ranges[i].IsSingle) sb.Append($"{e} == {rel.Ranges[i].Low}");
                else sb.Append($"({e} >= {rel.Ranges[i].Low} && {e} <= {rel.Ranges[i].High})");
            }
            if (p) sb.Append(')');
        }
    }
}

// ─── Category → enum name ───────────────────────────────────────────
static string[] CatOrder = { "Zero", "One", "Two", "Few", "Many" };

static string ParseCat(string name)
{
    switch(name) {
        case "zero": return "Zero"; case "one": return "One"; case "two": return "Two";
        case "few": return "Few"; case "many": return "Many"; case "other": return "Other";
        default: throw new Exception($"Unknown category: {name}");
    }
}

// ─── Main Generation ────────────────────────────────────────────────
string scriptDir = Path.GetDirectoryName(Path.GetFullPath(Args.Count > 0 ? Args[0] : "build/cldr/plurals.json")) ?? ".";
string jsonPath = Args.Count > 0 ? Args[0] : "build/cldr/plurals.json";
string json = File.ReadAllText(jsonPath);

var doc = JsonDocument.Parse(json);
var cardinals = doc.RootElement.GetProperty("supplemental").GetProperty("plurals-type-cardinal");

// Parse all locales
var allLocales = new Dictionary<string, Dictionary<string, OrExpr>>(); // locale → {cat → simplified}
var allCategories = new Dictionary<string, List<string>>(); // locale → [category names]

foreach (var localeProp in cardinals.EnumerateObject())
{
    var locale = localeProp.Name;
    var rules = new Dictionary<string, OrExpr>();
    var cats = new List<string>();

    foreach (var ruleProp in localeProp.Value.EnumerateObject())
    {
        var catName = ParseCat(ruleProp.Name.Substring("pluralRule-count-".Length));
        var condition = ruleProp.Value.GetString() ?? "";
        var ast = ParseRule(condition);
        var simplified = Simplify(ast);

        // Dead rule: simplified is null but ast was not → skip
        if (simplified == null && ast != null) continue;

        rules[catName] = simplified;
        cats.Add(catName);
    }

    if (!cats.Contains("Other")) cats.Add("Other");
    allLocales[locale] = rules;
    allCategories[locale] = cats;
}

// Group locales with identical rules
string ComputeKey(Dictionary<string, OrExpr> rules)
{
    var sb2 = new StringBuilder();
    foreach (var cat in rules.Keys.OrderBy(c => c))
        sb2.Append($"{cat}:{EmitCondition(rules[cat]) ?? "OTHER"};");
    return sb2.ToString();
}

var groups = new Dictionary<string, (List<string> Locales, Dictionary<string, OrExpr> Rules)>();
foreach (var kvp in allLocales)
{
    var key = ComputeKey(kvp.Value);
    if (!groups.ContainsKey(key))
        groups[key] = (new List<string>(), kvp.Value);
    groups[key].Locales.Add(kvp.Key);
}

// ─── Generate CldrPluralRules.generated.cs ──────────────────────────
var rulesSb = new StringBuilder();
rulesSb.AppendLine("// <auto-generated/>");
rulesSb.AppendLine("// Generated from CLDR plurals.json — do not edit by hand.");
rulesSb.AppendLine("#nullable enable");
rulesSb.AppendLine();
rulesSb.AppendLine("namespace MoonBuggy.Core.Plural");
rulesSb.AppendLine("{");
rulesSb.AppendLine("    internal static class CldrPluralRules");
rulesSb.AppendLine("    {");
rulesSb.AppendLine("        internal static PluralCategory GetCategory(string locale, long n)");
rulesSb.AppendLine("        {");
rulesSb.AppendLine("            switch (locale)");
rulesSb.AppendLine("            {");

foreach (var group in groups.Values.OrderBy(g => g.Locales.Min()))
{
    foreach (var locale in group.Locales.OrderBy(l => l))
        rulesSb.AppendLine($"                case \"{locale}\":");

    foreach (var cat in CatOrder)
    {
        if (!group.Rules.ContainsKey(cat)) continue;
        var cond = EmitCondition(group.Rules[cat]);
        if (cond == null)
            rulesSb.AppendLine($"                    return PluralCategory.{cat};");
        else
            rulesSb.AppendLine($"                    if ({cond}) return PluralCategory.{cat};");
    }
    rulesSb.AppendLine("                    return PluralCategory.Other;");
}

rulesSb.AppendLine("                default:");
rulesSb.AppendLine("                    if (n == 1) return PluralCategory.One;");
rulesSb.AppendLine("                    return PluralCategory.Other;");
rulesSb.AppendLine("            }");
rulesSb.AppendLine("        }");
rulesSb.AppendLine("    }");
rulesSb.AppendLine("}");

string rulesOutPath = "src/MoonBuggy.Core/Plural/CldrPluralRules.generated.cs";
File.WriteAllText(rulesOutPath, rulesSb.ToString());
Console.WriteLine($"Generated {rulesOutPath}");

// ─── Generate CldrPluralCategories.generated.cs ─────────────────────
var catsSb = new StringBuilder();
catsSb.AppendLine("// <auto-generated/>");
catsSb.AppendLine("// Generated from CLDR plurals.json — do not edit by hand.");
catsSb.AppendLine("#nullable enable");
catsSb.AppendLine();
catsSb.AppendLine("using System.Collections.Generic;");
catsSb.AppendLine();
catsSb.AppendLine("namespace MoonBuggy.Core.Plural");
catsSb.AppendLine("{");
catsSb.AppendLine("    internal static class CldrPluralCategories");
catsSb.AppendLine("    {");
catsSb.AppendLine("        private static readonly Dictionary<string, PluralCategory[]> _categories = new Dictionary<string, PluralCategory[]>");
catsSb.AppendLine("        {");

foreach (var kvp in allCategories.OrderBy(k => k.Key))
{
    var catEnums = kvp.Value
        .Select(c => (Name: c, Order: Array.IndexOf(new[] { "Zero", "One", "Two", "Few", "Many", "Other" }, c)))
        .OrderBy(x => x.Order)
        .Select(x => $"PluralCategory.{x.Name}");
    catsSb.AppendLine($"            {{ \"{kvp.Key}\", new[] {{ {string.Join(", ", catEnums)} }} }},");
}

catsSb.AppendLine("        };");
catsSb.AppendLine();
catsSb.AppendLine("        internal static PluralCategory[] GetCategories(string locale)");
catsSb.AppendLine("        {");
catsSb.AppendLine("            if (_categories.TryGetValue(locale, out var categories))");
catsSb.AppendLine("                return categories;");
catsSb.AppendLine("            return new[] { PluralCategory.One, PluralCategory.Other };");
catsSb.AppendLine("        }");
catsSb.AppendLine("    }");
catsSb.AppendLine("}");

string catsOutPath = "src/MoonBuggy.Core/Plural/CldrPluralCategories.generated.cs";
File.WriteAllText(catsOutPath, catsSb.ToString());
Console.WriteLine($"Generated {catsOutPath}");
Console.WriteLine($"Processed {allLocales.Count} locales in {groups.Count} rule groups.");
