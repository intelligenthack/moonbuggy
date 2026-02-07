using MoonBuggy.Cli.Commands;
using MoonBuggy.Core.Config;

if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
{
    PrintUsage();
    return 0;
}

var command = args[0];

switch (command)
{
    case "extract":
        return RunExtract(args.Skip(1).ToArray());
    case "validate":
        return RunValidate(args.Skip(1).ToArray());
    default:
        Console.Error.WriteLine($"Error: Unknown command '{command}'.");
        PrintUsage();
        return 1;
}

int RunExtract(string[] flags)
{
    var clean = false;
    var verbose = false;
    var watch = false;
    var locales = new List<string>();
    var files = new List<string>();

    for (var i = 0; i < flags.Length; i++)
    {
        switch (flags[i])
        {
            case "--clean":
                clean = true;
                break;
            case "--verbose":
            case "-v":
                verbose = true;
                break;
            case "--watch":
                watch = true;
                break;
            case "--locale":
                if (i + 1 < flags.Length)
                    locales.Add(flags[++i]);
                break;
            default:
                if (!flags[i].StartsWith("-"))
                    files.Add(flags[i]);
                break;
        }
    }

    var configPath = FindConfigFile();
    if (configPath == null)
    {
        Console.Error.WriteLine("Error: Could not find moonbuggy.config.json in current or parent directories.");
        return 1;
    }

    var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath))!;

    MoonBuggyConfig config;
    try
    {
        config = MoonBuggyConfig.Load(configPath);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error reading config: {ex.Message}");
        return 1;
    }

    if (config.Locales.Count == 0)
    {
        Console.Error.WriteLine("Error: No locales configured in moonbuggy.config.json.");
        return 1;
    }

    if (config.Catalogs.Count == 0)
    {
        Console.Error.WriteLine("Error: No catalogs configured in moonbuggy.config.json.");
        return 1;
    }

    // Resolve file paths relative to current directory
    var resolvedFiles = files.Count > 0
        ? files.Select(f => Path.GetFullPath(f)).ToList()
        : null;

    var localeFilter = locales.Count > 0 ? locales : null;

    if (verbose)
    {
        Console.WriteLine($"Config: {configPath}");
        Console.WriteLine($"Locales: {string.Join(", ", localeFilter ?? config.Locales)}");
        Console.WriteLine($"Catalogs: {config.Catalogs.Count}");
        if (resolvedFiles != null)
            Console.WriteLine($"Files: {string.Join(", ", files)}");
        if (watch)
            Console.WriteLine("Watch: enabled");
        Console.WriteLine();
    }

    try
    {
        var results = ExtractCommand.Execute(config, baseDirectory, clean,
            files: resolvedFiles, localeFilter: localeFilter);

        PrintExtractResults(results);
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }

    if (watch)
    {
        Console.WriteLine("Watching for changes... (press Ctrl+C to stop)");
        WatchAndExtract(config, baseDirectory, clean, resolvedFiles, localeFilter);
    }

    return 0;
}

void PrintExtractResults(Dictionary<string, ExtractResult> results)
{
    Console.WriteLine("Catalog                         Total   New  Obsolete");
    Console.WriteLine("-----------------------------------------------------");
    foreach (var (path, result) in results.OrderBy(r => r.Key))
    {
        var displayPath = path.Length > 30 ? "..." + path.Substring(path.Length - 27) : path;
        Console.WriteLine($"{displayPath,-32}{result.TotalMessages,5}{result.NewMessages,6}{result.ObsoleteRemoved,10}");
    }

    Console.WriteLine();
    Console.WriteLine("Done.");
}

void WatchAndExtract(MoonBuggyConfig config, string baseDirectory, bool clean,
    List<string>? files, List<string>? localeFilter)
{
    using var watcher = new FileSystemWatcher(baseDirectory);
    watcher.IncludeSubdirectories = true;
    watcher.Filter = "*.cs";
    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

    // Also watch .cshtml files
    using var cshtmlWatcher = new FileSystemWatcher(baseDirectory);
    cshtmlWatcher.IncludeSubdirectories = true;
    cshtmlWatcher.Filter = "*.cshtml";
    cshtmlWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

    var debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
    debounceTimer.Elapsed += (_, _) =>
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("Change detected, re-extracting...");
            var results = ExtractCommand.Execute(config, baseDirectory, clean,
                files: files, localeFilter: localeFilter);
            PrintExtractResults(results);
            Console.WriteLine("Watching for changes... (press Ctrl+C to stop)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during re-extraction: {ex.Message}");
        }
    };

    void OnChange(object sender, FileSystemEventArgs e)
    {
        debounceTimer.Stop();
        debounceTimer.Start();
    }

    watcher.Changed += OnChange;
    watcher.Created += OnChange;
    watcher.Renamed += (s, e) => OnChange(s, e);
    cshtmlWatcher.Changed += OnChange;
    cshtmlWatcher.Created += OnChange;
    cshtmlWatcher.Renamed += (s, e) => OnChange(s, e);

    watcher.EnableRaisingEvents = true;
    cshtmlWatcher.EnableRaisingEvents = true;

    // Block until Ctrl+C
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    try { Task.Delay(Timeout.Infinite, cts.Token).Wait(); }
    catch (AggregateException) { }

    Console.WriteLine();
    Console.WriteLine("Stopped watching.");
}

int RunValidate(string[] flags)
{
    var strict = flags.Contains("--strict");
    var verbose = flags.Contains("--verbose") || flags.Contains("-v");
    string? localeFilter = null;
    for (var i = 0; i < flags.Length; i++)
    {
        if (flags[i] == "--locale" && i + 1 < flags.Length)
        {
            localeFilter = flags[i + 1];
            break;
        }
    }

    var configPath = FindConfigFile();
    if (configPath == null)
    {
        Console.Error.WriteLine("Error: Could not find moonbuggy.config.json in current or parent directories.");
        return 1;
    }

    var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath))!;

    MoonBuggyConfig config;
    try
    {
        config = MoonBuggyConfig.Load(configPath);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error reading config: {ex.Message}");
        return 1;
    }

    if (config.Locales.Count == 0)
    {
        Console.Error.WriteLine("Error: No locales configured in moonbuggy.config.json.");
        return 1;
    }

    if (config.Catalogs.Count == 0)
    {
        Console.Error.WriteLine("Error: No catalogs configured in moonbuggy.config.json.");
        return 1;
    }

    if (verbose)
    {
        Console.WriteLine($"Config: {configPath}");
        Console.WriteLine($"Locales: {string.Join(", ", localeFilter != null ? new[] { localeFilter } : config.Locales.ToArray())}");
        Console.WriteLine($"Strict: {strict}");
        Console.WriteLine();
    }

    var results = ValidateCommand.Execute(config, baseDirectory, strict, localeFilter);

    var hasErrors = false;
    foreach (var (path, result) in results.OrderBy(r => r.Key))
    {
        if (verbose || result.Errors.Count > 0)
        {
            Console.WriteLine($"{path}: {result.TotalEntries} entries, {result.MissingCount} untranslated");
        }

        foreach (var error in result.Errors)
        {
            Console.Error.WriteLine($"  ERROR: {error}");
            hasErrors = true;
        }
    }

    if (hasErrors)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Validation failed.");
        return 1;
    }

    Console.WriteLine("Validation passed.");
    return 0;
}

string? FindConfigFile()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir != null)
    {
        var candidate = Path.Combine(dir, "moonbuggy.config.json");
        if (File.Exists(candidate))
            return candidate;
        dir = Path.GetDirectoryName(dir);
    }
    return null;
}

void PrintUsage()
{
    Console.WriteLine("Usage: moonbuggy <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  extract    Scan source files and update PO catalogs");
    Console.WriteLine("  validate   Validate PO catalogs for correctness");
    Console.WriteLine();
    Console.WriteLine("Extract options:");
    Console.WriteLine("  [files...]         Extract from specific files only (default: use config globs)");
    Console.WriteLine("  --locale <locale>  Extract only for the specified locale (repeatable)");
    Console.WriteLine("  --clean            Remove obsolete entries from PO files");
    Console.WriteLine("  --watch            Re-extract automatically when source files change");
    Console.WriteLine("  --verbose          Print detailed output");
    Console.WriteLine();
    Console.WriteLine("Validate options:");
    Console.WriteLine("  --strict           Fail on missing translations");
    Console.WriteLine("  --locale <locale>  Validate only the specified locale");
    Console.WriteLine("  --verbose          Print detailed output");
}
