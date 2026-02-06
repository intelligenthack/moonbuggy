using System.Text.Json;
using MoonBuggy.Core.Plural;

namespace MoonBuggy.Tests;

/// <summary>
/// Data-driven test that reads plurals.json and validates that GetCategories
/// returns the correct set of plural categories for each locale.
/// </summary>
public class CldrPluralCategoriesTests
{
    private static readonly string PluralsJsonPath = Path.Combine(
        FindRepoRoot(), "build", "cldr", "plurals.json");

    [Theory]
    [MemberData(nameof(GetAllLocaleCategories))]
    public void GetCategories_ReturnsExpectedCategoriesForLocale(string locale, PluralCategory[] expected)
    {
        var actual = CldrPluralCategories.GetCategories(locale);
        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> GetAllLocaleCategories()
    {
        var json = File.ReadAllText(PluralsJsonPath);
        using var doc = JsonDocument.Parse(json);
        var cardinals = doc.RootElement
            .GetProperty("supplemental")
            .GetProperty("plurals-type-cardinal");

        foreach (var localeProp in cardinals.EnumerateObject())
        {
            var locale = localeProp.Name;
            var categories = new List<PluralCategory>();

            foreach (var ruleProp in localeProp.Value.EnumerateObject())
            {
                var categoryName = ruleProp.Name.Substring("pluralRule-count-".Length);
                categories.Add(ParseCategory(categoryName));
            }

            // The generated code filters out dead-for-integers categories.
            // We need to match what the generator actually produces.
            // For integer-only types, some categories are dead (e.g., "many" in cs/cs
            // where the rule is "v != 0"). We filter them the same way the generator does.
            var integerCategories = FilterForIntegers(localeProp.Value, categories);

            if (!integerCategories.Contains(PluralCategory.Other))
                integerCategories.Add(PluralCategory.Other);

            var sorted = integerCategories
                .OrderBy(c => (int)c)
                .ToArray();

            yield return new object[] { locale, sorted };
        }
    }

    private static List<PluralCategory> FilterForIntegers(
        JsonElement localeRules, List<PluralCategory> allCategories)
    {
        var result = new List<PluralCategory>();
        foreach (var ruleProp in localeRules.EnumerateObject())
        {
            var categoryName = ruleProp.Name.Substring("pluralRule-count-".Length);
            var category = ParseCategory(categoryName);
            var ruleText = ruleProp.Value.GetString() ?? "";

            // Parse and simplify to check if dead for integers
            var ast = CldrRuleParser.Parse(ruleText);
            var simplified = IntegerSimplifier.Simplify(ast);

            // Dead rule: simplified is null but ast was not null → skip
            if (simplified == null && ast != null)
                continue;

            result.Add(category);
        }
        return result;
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
