using MoonBuggy.Core.Config;

namespace MoonBuggy.Core.Tests.Config;

public class MoonBuggyConfigTests
{
    [Fact]
    public void Load_ValidConfig_ParsesAllFields()
    {
        var json = @"{
            ""sourceLocale"": ""en"",
            ""locales"": [""es"", ""fr""],
            ""catalogs"": [
                {
                    ""path"": ""locales/{locale}/messages"",
                    ""include"": [""src/**/*.cs""]
                }
            ]
        }";

        var config = MoonBuggyConfig.Load(new StringReader(json));

        Assert.Equal("en", config.SourceLocale);
        Assert.Equal(new[] { "es", "fr" }, config.Locales);
        Assert.Single(config.Catalogs);
        Assert.Equal("locales/{locale}/messages", config.Catalogs[0].Path);
        Assert.Equal(new[] { "src/**/*.cs" }, config.Catalogs[0].Include);
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() =>
            MoonBuggyConfig.Load("/nonexistent/path/moonbuggy.config.json"));
    }

    [Fact]
    public void Load_MinimalConfig_DefaultsApplied()
    {
        var json = @"{
            ""sourceLocale"": ""en"",
            ""locales"": [""de""]
        }";

        var config = MoonBuggyConfig.Load(new StringReader(json));

        Assert.Equal("en", config.SourceLocale);
        Assert.Equal(new[] { "de" }, config.Locales);
        Assert.Empty(config.Catalogs);
    }

    [Fact]
    public void GetPoPath_ReplacesLocaleAndAppendsExtension()
    {
        var config = new MoonBuggyConfig();
        var catalog = new CatalogConfig { Path = "locales/{locale}/messages" };

        var result = config.GetPoPath(catalog, "es");

        Assert.Equal("locales/es/messages.po", result);
    }

    [Fact]
    public void GetPoPath_NoPlaceholder_AppendsExtension()
    {
        var config = new MoonBuggyConfig();
        var catalog = new CatalogConfig { Path = "messages" };

        var result = config.GetPoPath(catalog, "es");

        Assert.Equal("messages.po", result);
    }
}
