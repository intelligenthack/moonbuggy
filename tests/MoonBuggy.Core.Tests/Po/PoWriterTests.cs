using MoonBuggy.Core.Po;

namespace MoonBuggy.Core.Tests.Po;

public class PoWriterTests
{
    [Fact]
    public void Write_SingleSimpleEntry()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Save changes", MsgStr = "Guardar cambios" });

        var result = PoWriter.Write(catalog);

        Assert.Equal(
            "msgid \"Save changes\"\nmsgstr \"Guardar cambios\"\n",
            result);
    }

    [Fact]
    public void Write_UntranslatedEntry_EmptyMsgStr()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Save changes" });

        var result = PoWriter.Write(catalog);

        Assert.Equal(
            "msgid \"Save changes\"\nmsgstr \"\"\n",
            result);
    }

    [Fact]
    public void Write_EntryWithMsgCtxt()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry
        {
            MsgId = "Submit",
            MsgStr = "Enviar",
            MsgCtxt = "button"
        });

        var result = PoWriter.Write(catalog);

        Assert.Equal(
            "msgctxt \"button\"\nmsgid \"Submit\"\nmsgstr \"Enviar\"\n",
            result);
    }

    [Fact]
    public void Write_StringWithQuotesAndBackslashes_EscapedCorrectly()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry
        {
            MsgId = "She said \"hello\"",
            MsgStr = "Path: C:\\Users"
        });

        var result = PoWriter.Write(catalog);

        Assert.Contains("msgid \"She said \\\"hello\\\"\"", result);
        Assert.Contains("msgstr \"Path: C:\\\\Users\"", result);
    }

    [Fact]
    public void Write_StringWithEmbeddedNewlines_MultilinePoConcatenation()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry
        {
            MsgId = "Line one\nLine two",
            MsgStr = ""
        });

        var result = PoWriter.Write(catalog);

        Assert.Contains("msgid \"\"\n\"Line one\\n\"\n\"Line two\"\n", result);
    }

    [Fact]
    public void Write_EntryWithAllCommentTypes()
    {
        var catalog = new PoCatalog();
        var entry = new PoEntry { MsgId = "Hello", MsgStr = "Hola" };
        entry.TranslatorComments.Add("Greeting shown on login");
        entry.ExtractedComments.Add("auto-extracted");
        entry.References.Add("src/Views/Login.cshtml:10");
        entry.Flags.Add("fuzzy");
        catalog.Entries.Add(entry);

        var result = PoWriter.Write(catalog);

        var expected =
            "# Greeting shown on login\n" +
            "#. auto-extracted\n" +
            "#: src/Views/Login.cshtml:10\n" +
            "#, fuzzy\n" +
            "msgid \"Hello\"\n" +
            "msgstr \"Hola\"\n";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Write_HeaderAndMultipleEntries_BlankLineSeparated()
    {
        var catalog = new PoCatalog
        {
            Header = new PoEntry
            {
                MsgId = "",
                MsgStr = "Content-Type: text/plain; charset=UTF-8\n"
            }
        };
        catalog.Entries.Add(new PoEntry { MsgId = "Hello", MsgStr = "Hola" });
        catalog.Entries.Add(new PoEntry { MsgId = "Bye", MsgStr = "Adiós" });

        var result = PoWriter.Write(catalog);

        var expected =
            "msgid \"\"\n" +
            "msgstr \"\"\n" +
            "\"Content-Type: text/plain; charset=UTF-8\\n\"\n" +
            "\n" +
            "msgid \"Hello\"\n" +
            "msgstr \"Hola\"\n" +
            "\n" +
            "msgid \"Bye\"\n" +
            "msgstr \"Adiós\"\n";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Write_EmptyCatalog_EmptyString()
    {
        var catalog = new PoCatalog();

        var result = PoWriter.Write(catalog);

        Assert.Equal("", result);
    }
}
