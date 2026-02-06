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
        Console.Error.WriteLine("Error: 'validate' command is not yet implemented.");
        return 1;
    default:
        Console.Error.WriteLine($"Error: Unknown command '{command}'.");
        PrintUsage();
        return 1;
}

int RunExtract(string[] flags)
{
    var clean = flags.Contains("--clean");
    var verbose = flags.Contains("--verbose") || flags.Contains("-v");

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
        Console.WriteLine($"Locales: {string.Join(", ", config.Locales)}");
        Console.WriteLine($"Catalogs: {config.Catalogs.Count}");
        Console.WriteLine();
    }

    var results = ExtractCommand.Execute(config, baseDirectory, clean);

    // Print results table
    Console.WriteLine("Catalog                         Total   New  Obsolete");
    Console.WriteLine("-----------------------------------------------------");
    foreach (var (path, result) in results.OrderBy(r => r.Key))
    {
        var displayPath = path.Length > 30 ? "..." + path.Substring(path.Length - 27) : path;
        Console.WriteLine($"{displayPath,-32}{result.TotalMessages,5}{result.NewMessages,6}{result.ObsoleteRemoved,10}");
    }

    Console.WriteLine();
    Console.WriteLine("Done.");
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
    Console.WriteLine("  validate   Validate PO catalogs against source (not yet implemented)");
    Console.WriteLine();
    Console.WriteLine("Extract options:");
    Console.WriteLine("  --clean    Remove obsolete entries from PO files");
    Console.WriteLine("  --verbose  Print detailed output");
}
