using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonBuggy.Core.Config;
using MoonBuggy.Core.Icu;
using MoonBuggy.Core.Plural;
using MoonBuggy.Core.Po;

namespace MoonBuggy.Cli.Commands;

public class ValidateResult
{
    public int TotalEntries { get; set; }
    public int MissingCount { get; set; }
    public List<string> Errors { get; } = new();
}

public static class ValidateCommand
{
    public static Dictionary<string, ValidateResult> Execute(
        MoonBuggyConfig config, string baseDirectory, bool strict = false, string? localeFilter = null)
    {
        var results = new Dictionary<string, ValidateResult>();

        var locales = localeFilter != null
            ? config.Locales.Where(l => l == localeFilter).ToList()
            : config.Locales;

        foreach (var catalogConfig in config.Catalogs)
        {
            foreach (var locale in locales)
            {
                var poRelativePath = config.GetPoPath(catalogConfig, locale);
                var poFullPath = Path.Combine(baseDirectory, poRelativePath);

                var result = new ValidateResult();

                if (!File.Exists(poFullPath))
                {
                    results[poRelativePath] = result;
                    continue;
                }

                var catalog = PoReader.Read(File.ReadAllText(poFullPath));
                result.TotalEntries = catalog.Entries.Count;

                foreach (var entry in catalog.Entries)
                {
                    if (string.IsNullOrEmpty(entry.MsgStr))
                    {
                        result.MissingCount++;
                        if (strict)
                        {
                            result.Errors.Add($"Missing translation for \"{entry.MsgId}\"");
                        }
                        continue;
                    }

                    // Parse msgid ICU
                    IcuNode[] msgIdNodes;
                    try
                    {
                        msgIdNodes = IcuParser.Parse(entry.MsgId);
                    }
                    catch
                    {
                        // If msgid itself is unparseable, skip checks on this entry
                        continue;
                    }

                    // Parse msgstr ICU
                    IcuNode[] msgStrNodes;
                    try
                    {
                        msgStrNodes = IcuParser.Parse(entry.MsgStr);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Invalid ICU syntax in msgstr for \"{entry.MsgId}\": {ex.Message}");
                        continue;
                    }

                    // Variable check: compare variable names
                    var msgIdVars = CollectVariables(msgIdNodes);
                    var msgStrVars = CollectVariables(msgStrNodes);

                    foreach (var v in msgIdVars)
                    {
                        if (!msgStrVars.Contains(v))
                            result.Errors.Add($"Missing variable {{{v}}} in msgstr for \"{entry.MsgId}\"");
                    }

                    foreach (var v in msgStrVars)
                    {
                        if (!msgIdVars.Contains(v))
                            result.Errors.Add($"Extra variable {{{v}}} in msgstr for \"{entry.MsgId}\"");
                    }

                    // Plural form check for target locale
                    ValidatePluralForms(msgStrNodes, locale, entry.MsgId, result);
                }

                results[poRelativePath] = result;
            }
        }

        return results;
    }

    private static HashSet<string> CollectVariables(IcuNode[] nodes)
    {
        var vars = new HashSet<string>();
        CollectVariablesRecursive(nodes, vars);
        return vars;
    }

    private static void CollectVariablesRecursive(IcuNode[] nodes, HashSet<string> vars)
    {
        foreach (var node in nodes)
        {
            if (node is IcuVariableNode variable)
            {
                vars.Add(variable.Name);
            }
            else if (node is IcuPluralNode plural)
            {
                vars.Add(plural.Variable);
                foreach (var branch in plural.Branches)
                {
                    CollectVariablesRecursive(branch.Content, vars);
                }
            }
        }
    }

    private static void ValidatePluralForms(IcuNode[] nodes, string locale, string msgId, ValidateResult result)
    {
        foreach (var node in nodes)
        {
            if (node is IcuPluralNode plural)
            {
                if (!CldrPluralRuleConditions.HasLocale(locale))
                    continue;

                var requiredCategories = CldrPluralRuleConditions.GetRequiredCategories(locale).ToList();
                var presentCategories = new HashSet<string>(plural.Branches.Select(b => b.Category));

                foreach (var required in requiredCategories)
                {
                    var categoryName = required.ToString().ToLowerInvariant();
                    // Also check for =0 as an alias for zero
                    if (!presentCategories.Contains(categoryName) &&
                        !(required == PluralCategory.Zero && presentCategories.Contains("=0")))
                    {
                        result.Errors.Add($"Missing plural form \"{categoryName}\" for locale \"{locale}\" in msgstr for \"{msgId}\"");
                    }
                }

                // Recurse into branch content
                foreach (var branch in plural.Branches)
                {
                    ValidatePluralForms(branch.Content, locale, msgId, result);
                }
            }
        }
    }
}
