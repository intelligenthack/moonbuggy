using MoonBuggy.Cli;

namespace MoonBuggy.Cli.Tests;

public class SourceScannerTests
{
    [Fact]
    public void ScanText_SimpleStringLiteral_ExtractsMessage()
    {
        var source = @"var x = _t(""Save changes"");";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("Save changes", msg.MbSyntax);
        Assert.Null(msg.Context);
        Assert.Equal("test.cs", msg.FilePath);
    }

    [Fact]
    public void ScanText_StringWithVariables_ExtractsRawMbSyntax()
    {
        var source = @"var x = _t(""Welcome to $name$!"", new { name });";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("Welcome to $name$!", msg.MbSyntax);
    }

    [Fact]
    public void ScanText_VerbatimString_ExtractsMessage()
    {
        var source = "var x = _t(@\"verbatim \"\"quoted\"\"\");";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("verbatim \"quoted\"", msg.MbSyntax);
    }

    [Fact]
    public void ScanText_NamedContext_ExtractsContext()
    {
        var source = @"var x = _t(""Submit"", context: ""button"");";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("Submit", msg.MbSyntax);
        Assert.Equal("button", msg.Context);
    }

    [Fact]
    public void ScanText_PositionalContext_ExtractsContext()
    {
        var source = @"var x = _t(""Submit"", null, ""button"");";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("Submit", msg.MbSyntax);
        Assert.Equal("button", msg.Context);
    }

    [Fact]
    public void ScanText_NonLiteralFirstArg_SkipsCall()
    {
        var source = @"var x = _t(variable);";

        var results = SourceScanner.ScanText(source, "test.cs");

        Assert.Empty(results);
    }

    [Fact]
    public void ScanText_MultipleCalls_ExtractsAllWithLineNumbers()
    {
        var source = @"var a = _t(""First"");
var b = something();
var c = _t(""Second"");
var d = _t(""Third"");";

        var results = SourceScanner.ScanText(source, "test.cs");

        Assert.Equal(3, results.Count);
        Assert.Equal("First", results[0].MbSyntax);
        Assert.Equal(1, results[0].LineNumber);
        Assert.Equal("Second", results[1].MbSyntax);
        Assert.Equal(3, results[1].LineNumber);
        Assert.Equal("Third", results[2].MbSyntax);
        Assert.Equal(4, results[2].LineNumber);
    }

    [Fact]
    public void ScanText_EscapedQuotes_ExtractsCorrectly()
    {
        var source = @"var x = _t(""She said \""hello\"""");";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("She said \"hello\"", msg.MbSyntax);
    }

    [Fact]
    public void ScanText_IgnoresMCalls()
    {
        var source = @"var a = _m(""bold"");
var b = _t(""plain"");";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("plain", msg.MbSyntax);
    }

    [Fact]
    public void ScanText_PluralSyntax_ExtractsRawMb()
    {
        var source = @"var x = _t(""$You have #count# item|You have #count# items$"", new { count });";

        var results = SourceScanner.ScanText(source, "test.cs");

        var msg = Assert.Single(results);
        Assert.Equal("$You have #count# item|You have #count# items$", msg.MbSyntax);
    }
}
