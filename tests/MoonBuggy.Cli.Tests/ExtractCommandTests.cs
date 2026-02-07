using MoonBuggy.Cli.Commands;
using MoonBuggy.Core.Config;
using MoonBuggy.Core.Po;

namespace MoonBuggy.Cli.Tests;

public class ExtractCommandTests : IDisposable
{
    private readonly string _tempDir;

    public ExtractCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "moonbuggy-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void WriteSourceFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private string ReadPoFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(_tempDir, relativePath));
    }

    private void WritePoFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private MoonBuggyConfig MakeConfig(params string[] locales)
    {
        return new MoonBuggyConfig
        {
            SourceLocale = "en",
            Locales = new List<string>(locales),
            Catalogs = new List<CatalogConfig>
            {
                new CatalogConfig
                {
                    Path = "locales/{locale}/messages",
                    Include = new List<string> { "src/**/*.cs" }
                }
            }
        };
    }

    [Fact]
    public void Execute_NewProject_CreatesPoFiles()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Save changes"");");
        var config = MakeConfig("es", "fr");

        ExtractCommand.Execute(config, _tempDir);

        // PO files should exist for both locales
        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/es/messages.po")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/fr/messages.po")));

        // Verify content
        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        var entry = catalog.Find("Save changes");
        Assert.NotNull(entry);
        Assert.Equal("", entry!.MsgStr); // empty msgstr for new entry
    }

    [Fact]
    public void Execute_PreservesExistingTranslations()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Save changes"");");

        var existingPo = "msgid \"Save changes\"\nmsgstr \"Guardar cambios\"\n";
        WritePoFile("locales/es/messages.po", existingPo);

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        var entry = catalog.Find("Save changes");
        Assert.NotNull(entry);
        Assert.Equal("Guardar cambios", entry!.MsgStr);
    }

    [Fact]
    public void Execute_CleanRemovesObsolete()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Keep me"");");

        var existingPo = "msgid \"Keep me\"\nmsgstr \"Mantenerme\"\n\nmsgid \"Remove me\"\nmsgstr \"Eliminarme\"\n";
        WritePoFile("locales/es/messages.po", existingPo);

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir, clean: true);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Keep me"));
        Assert.Null(catalog.Find("Remove me"));
    }

    [Fact]
    public void Execute_WithoutClean_PreservesObsolete()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Keep me"");");

        var existingPo = "msgid \"Keep me\"\nmsgstr \"Mantenerme\"\n\nmsgid \"Remove me\"\nmsgstr \"Eliminarme\"\n";
        WritePoFile("locales/es/messages.po", existingPo);

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir, clean: false);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Keep me"));
        Assert.NotNull(catalog.Find("Remove me"));
    }

    [Fact]
    public void Execute_ContextMessages_SeparateEntries()
    {
        WriteSourceFile("src/App.cs",
            @"var a = _t(""Submit"", context: ""button"");
var b = _t(""Submit"", context: ""menu"");");

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Submit", "button"));
        Assert.NotNull(catalog.Find("Submit", "menu"));
        Assert.Equal(2, catalog.Entries.Count);
    }

    [Fact]
    public void Execute_MbSyntax_TransformedToIcu()
    {
        WriteSourceFile("src/App.cs",
            @"var x = _t(""$You have #count# item|You have #count# items$"", new { count });");

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        // MB syntax should be transformed to ICU MessageFormat
        var entry = catalog.Entries[0];
        Assert.Contains("{count, plural,", entry.MsgId);
        Assert.Contains("one {", entry.MsgId);
        Assert.Contains("other {", entry.MsgId);
    }

    [Fact]
    public void Execute_DuplicateMessages_SingleEntry()
    {
        WriteSourceFile("src/FileA.cs", @"var x = _t(""Shared message"");");
        WriteSourceFile("src/FileB.cs", @"var x = _t(""Shared message"");");

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.Single(catalog.Entries);
        Assert.Equal(2, catalog.Entries[0].References.Count);
    }

    [Fact]
    public void Execute_MCall_MarkdownTransformed()
    {
        WriteSourceFile("src/App.cs", @"var x = _m(""Click **here**"");");

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        var entry = catalog.Entries[0];
        Assert.Equal("Click <0>here</0>", entry.MsgId);
    }

    [Fact]
    public void Execute_ReturnsCorrectStatistics()
    {
        WriteSourceFile("src/App.cs",
            @"var a = _t(""New message"");
var b = _t(""Existing"");");

        var existingPo = "msgid \"Existing\"\nmsgstr \"Existente\"\n\nmsgid \"Obsolete\"\nmsgstr \"Obsoleto\"\n";
        WritePoFile("locales/es/messages.po", existingPo);

        var config = MakeConfig("es");
        var results = ExtractCommand.Execute(config, _tempDir, clean: true);

        Assert.Single(results);
        var result = results["locales/es/messages.po"];
        Assert.Equal(2, result.TotalMessages);
        Assert.Equal(1, result.NewMessages);
        Assert.Equal(1, result.ObsoleteRemoved);
    }

    // --- File arguments: extract from specific files only ---

    [Fact]
    public void Execute_WithFiles_ExtractsOnlyFromSpecifiedFiles()
    {
        WriteSourceFile("src/FileA.cs", @"var x = _t(""Hello"");");
        WriteSourceFile("src/FileB.cs", @"var x = _t(""World"");");

        var config = MakeConfig("es");
        var files = new[] { Path.Combine(_tempDir, "src/FileA.cs") };
        ExtractCommand.Execute(config, _tempDir, files: files);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Hello"));
        Assert.Null(catalog.Find("World"));
    }

    [Fact]
    public void Execute_WithMultipleFiles_ExtractsFromAll()
    {
        WriteSourceFile("src/FileA.cs", @"var x = _t(""Hello"");");
        WriteSourceFile("src/FileB.cs", @"var x = _t(""World"");");

        var config = MakeConfig("es");
        var files = new[]
        {
            Path.Combine(_tempDir, "src/FileA.cs"),
            Path.Combine(_tempDir, "src/FileB.cs")
        };
        ExtractCommand.Execute(config, _tempDir, files: files);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Hello"));
        Assert.NotNull(catalog.Find("World"));
    }

    [Fact]
    public void Execute_WithNonExistentFile_ThrowsFileNotFound()
    {
        var config = MakeConfig("es");
        var files = new[] { Path.Combine(_tempDir, "NonExistent.cs") };

        Assert.Throws<FileNotFoundException>(() =>
            ExtractCommand.Execute(config, _tempDir, files: files));
    }

    [Fact]
    public void Execute_WithNoFiles_UsesConfigGlobs()
    {
        WriteSourceFile("src/FileA.cs", @"var x = _t(""Hello"");");
        WriteSourceFile("src/FileB.cs", @"var x = _t(""World"");");

        var config = MakeConfig("es");
        ExtractCommand.Execute(config, _tempDir);

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Hello"));
        Assert.NotNull(catalog.Find("World"));
    }

    // --- Locale filter: extract for specific locales only ---

    [Fact]
    public void Execute_WithLocaleFilter_OnlyUpdatesSpecifiedLocale()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Hello"");");

        var config = MakeConfig("en", "es", "ru");
        ExtractCommand.Execute(config, _tempDir, localeFilter: new[] { "es" });

        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/es/messages.po")));
        Assert.False(File.Exists(Path.Combine(_tempDir, "locales/en/messages.po")));
        Assert.False(File.Exists(Path.Combine(_tempDir, "locales/ru/messages.po")));
    }

    [Fact]
    public void Execute_WithMultipleLocaleFilters_UpdatesSpecifiedLocales()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Hello"");");

        var config = MakeConfig("en", "es", "ru");
        ExtractCommand.Execute(config, _tempDir, localeFilter: new[] { "es", "ru" });

        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/es/messages.po")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/ru/messages.po")));
        Assert.False(File.Exists(Path.Combine(_tempDir, "locales/en/messages.po")));
    }

    [Fact]
    public void Execute_WithInvalidLocaleFilter_ThrowsArgumentException()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Hello"");");

        var config = MakeConfig("en", "es");

        Assert.Throws<ArgumentException>(() =>
            ExtractCommand.Execute(config, _tempDir, localeFilter: new[] { "xx" }));
    }

    [Fact]
    public void Execute_WithNoLocaleFilter_UpdatesAllLocales()
    {
        WriteSourceFile("src/App.cs", @"var x = _t(""Hello"");");

        var config = MakeConfig("en", "es");
        ExtractCommand.Execute(config, _tempDir);

        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/en/messages.po")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/es/messages.po")));
    }

    // --- Flag combinations ---

    [Fact]
    public void Execute_FilesAndLocaleFilter_BothApply()
    {
        WriteSourceFile("src/FileA.cs", @"var x = _t(""Hello"");");
        WriteSourceFile("src/FileB.cs", @"var x = _t(""World"");");

        var config = MakeConfig("en", "es");
        var files = new[] { Path.Combine(_tempDir, "src/FileA.cs") };
        ExtractCommand.Execute(config, _tempDir, files: files, localeFilter: new[] { "es" });

        // Only FileA scanned, only es locale updated
        Assert.True(File.Exists(Path.Combine(_tempDir, "locales/es/messages.po")));
        Assert.False(File.Exists(Path.Combine(_tempDir, "locales/en/messages.po")));

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Hello"));
        Assert.Null(catalog.Find("World"));
    }

    [Fact]
    public void Execute_FilesAndCleanAndLocaleFilter_AllApply()
    {
        WriteSourceFile("src/FileA.cs", @"var x = _t(""Hello"");");

        var existingPo = "msgid \"Obsolete\"\nmsgstr \"Obsoleto\"\n";
        WritePoFile("locales/es/messages.po", existingPo);

        var config = MakeConfig("en", "es");
        var files = new[] { Path.Combine(_tempDir, "src/FileA.cs") };
        ExtractCommand.Execute(config, _tempDir, clean: true, files: files, localeFilter: new[] { "es" });

        var catalog = PoReader.Read(ReadPoFile("locales/es/messages.po"));
        Assert.NotNull(catalog.Find("Hello"));
        Assert.Null(catalog.Find("Obsolete"));
        Assert.False(File.Exists(Path.Combine(_tempDir, "locales/en/messages.po")));
    }
}
