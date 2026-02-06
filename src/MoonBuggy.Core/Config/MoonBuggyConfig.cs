using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MoonBuggy.Core.Config;

public class CatalogConfig
{
    public string Path { get; set; } = "";
    public List<string> Include { get; set; } = new List<string>();
}

public class MoonBuggyConfig
{
    public string SourceLocale { get; set; } = "en";
    public List<string> Locales { get; set; } = new List<string>();
    public List<CatalogConfig> Catalogs { get; set; } = new List<CatalogConfig>();

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static MoonBuggyConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException("Config file not found.", configPath);

        using (var reader = new StreamReader(configPath))
        {
            return Load(reader);
        }
    }

    public static MoonBuggyConfig Load(TextReader reader)
    {
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<MoonBuggyConfig>(json, JsonOptions)
               ?? new MoonBuggyConfig();
    }

    public string GetPoPath(CatalogConfig catalog, string locale)
    {
        var path = catalog.Path.Replace("{locale}", locale);
        return path + ".po";
    }
}
