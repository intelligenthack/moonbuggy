using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonBuggy.Core.Config;
using MoonBuggy.Core.Icu;
using MoonBuggy.Core.Markdown;
using MoonBuggy.Core.Po;

namespace MoonBuggy.Cli.Commands;

public class ExtractResult
{
    public int TotalMessages { get; set; }
    public int NewMessages { get; set; }
    public int ObsoleteRemoved { get; set; }
}

public static class ExtractCommand
{
    public static Dictionary<string, ExtractResult> Execute(
        MoonBuggyConfig config, string baseDirectory, bool clean = false,
        IReadOnlyList<string>? files = null, IReadOnlyList<string>? localeFilter = null)
    {
        // Validate locale filter
        if (localeFilter != null)
        {
            foreach (var locale in localeFilter)
            {
                if (!config.Locales.Contains(locale))
                    throw new System.ArgumentException($"Locale '{locale}' is not configured. Available locales: {string.Join(", ", config.Locales)}");
            }
        }

        var allResults = new Dictionary<string, ExtractResult>();

        foreach (var catalogConfig in config.Catalogs)
        {
            // 1. Scan source files — use explicit files if provided, otherwise use config globs
            IReadOnlyList<ExtractedMessage> messages;
            if (files != null && files.Count > 0)
            {
                var allMessages = new List<ExtractedMessage>();
                foreach (var file in files)
                {
                    if (!File.Exists(file))
                        throw new FileNotFoundException($"Source file not found: {file}", file);
                    allMessages.AddRange(SourceScanner.ScanFile(file));
                }
                messages = allMessages;
            }
            else
            {
                messages = SourceScanner.ScanFiles(baseDirectory, catalogConfig.Include);
            }

            // 2. Transform MB syntax to ICU and collect unique entries
            var icuMessages = new List<(string MsgId, string? MsgCtxt, string FilePath, int LineNumber)>();
            foreach (var msg in messages)
            {
                var icuMsgId = msg.IsMarkdown
                    ? MarkdownPlaceholderExtractor.ToIcuWithMarkdown(msg.MbSyntax)
                    : IcuTransformer.ToIcu(msg.MbSyntax);
                icuMessages.Add((icuMsgId, msg.Context, msg.FilePath, msg.LineNumber));
            }

            // 3. Build active key set
            var activeKeys = new HashSet<(string MsgId, string? MsgCtxt)>(
                icuMessages.Select(m => (m.MsgId, m.MsgCtxt)));

            // 4. For each locale (filtered if specified), merge into PO catalog
            var localesToProcess = localeFilter != null
                ? config.Locales.Where(l => localeFilter.Contains(l)).ToList()
                : config.Locales;

            foreach (var locale in localesToProcess)
            {
                var poRelativePath = config.GetPoPath(catalogConfig, locale);
                var poFullPath = Path.Combine(baseDirectory, poRelativePath);

                // Load existing catalog or create new
                PoCatalog catalog;
                if (File.Exists(poFullPath))
                    catalog = PoReader.Read(File.ReadAllText(poFullPath));
                else
                    catalog = new PoCatalog();

                var existingCount = catalog.Entries.Count;

                // Clear references on all existing entries (rebuild from current source)
                foreach (var entry in catalog.Entries)
                    entry.References.Clear();

                // Merge messages
                foreach (var msg in icuMessages)
                {
                    var entry = catalog.GetOrAdd(msg.MsgId, msg.MsgCtxt);
                    // Make relative path from base dir for references
                    var relativePath = GetRelativePath(baseDirectory, msg.FilePath);
                    var reference = $"{relativePath}:{msg.LineNumber}";
                    if (!entry.References.Contains(reference))
                        entry.References.Add(reference);
                }

                var newCount = catalog.Entries.Count - existingCount;

                // Optionally remove obsolete entries
                var obsoleteRemoved = 0;
                if (clean)
                    obsoleteRemoved = catalog.RemoveObsolete(activeKeys);

                // Write output
                var dir = Path.GetDirectoryName(poFullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(poFullPath, PoWriter.Write(catalog));

                allResults[poRelativePath] = new ExtractResult
                {
                    TotalMessages = catalog.Entries.Count,
                    NewMessages = newCount,
                    ObsoleteRemoved = obsoleteRemoved
                };
            }
        }

        return allResults;
    }

    private static string GetRelativePath(string basePath, string fullPath)
    {
        // Normalize separators
        basePath = Path.GetFullPath(basePath);
        fullPath = Path.GetFullPath(fullPath);

        if (fullPath.StartsWith(basePath))
        {
            var relative = fullPath.Substring(basePath.Length);
            if (relative.Length > 0 && (relative[0] == Path.DirectorySeparatorChar || relative[0] == Path.AltDirectorySeparatorChar))
                relative = relative.Substring(1);
            return relative.Replace('\\', '/');
        }

        return fullPath.Replace('\\', '/');
    }
}
