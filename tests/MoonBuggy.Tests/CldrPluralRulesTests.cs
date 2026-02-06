using System.Text.Json;
using MoonBuggy.Core.Plural;

namespace MoonBuggy.Tests;

/// <summary>
/// Data-driven test that reads plurals.json, parses ALL @integer samples
/// for ALL locales, and validates GetCategory against the CLDR source of truth.
/// </summary>
public class CldrPluralRulesTests
{
    private static readonly string PluralsJsonPath = Path.Combine(
        FindRepoRoot(), "build", "cldr", "plurals.json");

    [Theory]
    [MemberData(nameof(GetAllIntegerSamples))]
    public void GetCategory_MatchesCldrIntegerSamples(string locale, long value, PluralCategory expected)
    {
        Assert.Equal(expected, CldrPluralRules.GetCategory(locale, value));
    }

    public static IEnumerable<object[]> GetAllIntegerSamples()
    {
        var json = File.ReadAllText(PluralsJsonPath);
        using var doc = JsonDocument.Parse(json);
        var cardinals = doc.RootElement
            .GetProperty("supplemental")
            .GetProperty("plurals-type-cardinal");

        foreach (var localeProp in cardinals.EnumerateObject())
        {
            var locale = localeProp.Name;
            foreach (var ruleProp in localeProp.Value.EnumerateObject())
            {
                var categoryName = ruleProp.Name.Substring("pluralRule-count-".Length);
                var category = ParseCategory(categoryName);
                var ruleText = ruleProp.Value.GetString() ?? "";

                foreach (var value in ParseIntegerSamples(ruleText))
                {
                    yield return new object[] { locale, value, category };
                }
            }
        }
    }

    /// <summary>
    /// Parses @integer samples from a CLDR rule string.
    /// Format: "...condition... @integer 0, 2~16, 100, 1000, 10000, ..."
    /// Handles individual values, ranges (2~16), and skips ellipsis (...).
    /// Also handles compact notation like 1c3 (= 1000), 2c6 (= 2000000).
    /// </summary>
    private static IEnumerable<long> ParseIntegerSamples(string ruleText)
    {
        var intIdx = ruleText.IndexOf("@integer");
        if (intIdx < 0) yield break;

        var sampleText = ruleText.Substring(intIdx + "@integer".Length);

        // Stop at @decimal if present
        var decIdx = sampleText.IndexOf("@decimal");
        if (decIdx >= 0)
            sampleText = sampleText.Substring(0, decIdx);

        var parts = sampleText.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed == "…" || trimmed == "...") continue;

            // Strip trailing ellipsis: "1000000, …" → "1000000"
            trimmed = trimmed.TrimEnd('…', '.', ' ');
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (trimmed.Contains('~'))
            {
                // Range: "2~16" means 2,3,4,...,16
                var rangeParts = trimmed.Split('~');
                var lo = ParseCompactLong(rangeParts[0].Trim());
                var hi = ParseCompactLong(rangeParts[1].Trim());
                if (lo == null || hi == null) continue;
                for (long v = lo.Value; v <= hi.Value; v++)
                    yield return v;
            }
            else
            {
                var val = ParseCompactLong(trimmed);
                if (val != null)
                    yield return val.Value;
            }
        }
    }

    /// <summary>
    /// Parses a long value, handling CLDR compact notation (e.g., "1c6" = 1000000).
    /// </summary>
    private static long? ParseCompactLong(string s)
    {
        s = s.Trim();
        if (string.IsNullOrEmpty(s)) return null;

        var cIdx = s.IndexOf('c');
        if (cIdx >= 0)
        {
            if (!long.TryParse(s.Substring(0, cIdx), out var mantissa)) return null;
            if (!int.TryParse(s.Substring(cIdx + 1), out var exp)) return null;
            return mantissa * (long)Math.Pow(10, exp);
        }

        return long.TryParse(s, out var result) ? result : (long?)null;
    }

    private static PluralCategory ParseCategory(string name)
    {
        switch (name)
        {
            case "zero": return PluralCategory.Zero;
            case "one": return PluralCategory.One;
            case "two": return PluralCategory.Two;
            case "few": return PluralCategory.Few;
            case "many": return PluralCategory.Many;
            case "other": return PluralCategory.Other;
            default: throw new ArgumentException($"Unknown plural category: {name}");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "MoonBuggy.slnx")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new InvalidOperationException("Could not find repo root (MoonBuggy.slnx)");
    }
}
