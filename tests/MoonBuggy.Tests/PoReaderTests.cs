using MoonBuggy.Core.Po;

namespace MoonBuggy.Tests;

public class PoReaderTests
{
    [Fact]
    public void Read_SingleSimpleEntry()
    {
        var po = "msgid \"Save changes\"\nmsgstr \"Guardar cambios\"\n";

        var catalog = PoReader.Read(po);

        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("Save changes", entry.MsgId);
        Assert.Equal("Guardar cambios", entry.MsgStr);
        Assert.Null(entry.MsgCtxt);
    }

    [Fact]
    public void Read_EntryWithMsgCtxt()
    {
        var po = "msgctxt \"button\"\nmsgid \"Submit\"\nmsgstr \"Enviar\"\n";

        var catalog = PoReader.Read(po);

        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("Submit", entry.MsgId);
        Assert.Equal("Enviar", entry.MsgStr);
        Assert.Equal("button", entry.MsgCtxt);
    }

    [Fact]
    public void Read_EscapedQuotesAndBackslashes()
    {
        var po = "msgid \"She said \\\"hello\\\"\"\nmsgstr \"Path: C:\\\\Users\"\n";

        var catalog = PoReader.Read(po);

        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("She said \"hello\"", entry.MsgId);
        Assert.Equal("Path: C:\\Users", entry.MsgStr);
    }

    [Fact]
    public void Read_MultilineConcatenation()
    {
        var po =
            "msgid \"\"\n" +
            "\"Line one\\n\"\n" +
            "\"Line two\"\n" +
            "msgstr \"\"\n";

        var catalog = PoReader.Read(po);

        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("Line one\nLine two", entry.MsgId);
    }

    [Fact]
    public void Read_AllCommentTypes()
    {
        var po =
            "# Greeting shown on login\n" +
            "#. auto-extracted\n" +
            "#: src/Views/Login.cshtml:10\n" +
            "#, fuzzy\n" +
            "msgid \"Hello\"\n" +
            "msgstr \"Hola\"\n";

        var catalog = PoReader.Read(po);

        var entry = Assert.Single(catalog.Entries);
        Assert.Single(entry.TranslatorComments, "Greeting shown on login");
        Assert.Single(entry.ExtractedComments, "auto-extracted");
        Assert.Single(entry.References, "src/Views/Login.cshtml:10");
        Assert.Single(entry.Flags, "fuzzy");
    }

    [Fact]
    public void Read_FullFileWithHeaderAndMultipleEntries()
    {
        var po =
            "msgid \"\"\n" +
            "msgstr \"Content-Type: text/plain; charset=UTF-8\\n\"\n" +
            "\n" +
            "msgid \"Hello\"\n" +
            "msgstr \"Hola\"\n" +
            "\n" +
            "msgid \"Bye\"\n" +
            "msgstr \"Adiós\"\n";

        var catalog = PoReader.Read(po);

        Assert.NotNull(catalog.Header);
        Assert.Equal("Content-Type: text/plain; charset=UTF-8\n", catalog.Header!.MsgStr);
        Assert.Equal(2, catalog.Entries.Count);
        Assert.Equal("Hello", catalog.Entries[0].MsgId);
        Assert.Equal("Bye", catalog.Entries[1].MsgId);
    }

    [Fact]
    public void Read_SameMsgIdDifferentContexts_SeparateEntries()
    {
        var po =
            "msgctxt \"button\"\n" +
            "msgid \"Submit\"\n" +
            "msgstr \"Enviar\"\n" +
            "\n" +
            "msgctxt \"legal\"\n" +
            "msgid \"Submit\"\n" +
            "msgstr \"Presentar\"\n";

        var catalog = PoReader.Read(po);

        Assert.Equal(2, catalog.Entries.Count);
        Assert.Equal("button", catalog.Entries[0].MsgCtxt);
        Assert.Equal("Enviar", catalog.Entries[0].MsgStr);
        Assert.Equal("legal", catalog.Entries[1].MsgCtxt);
        Assert.Equal("Presentar", catalog.Entries[1].MsgStr);
    }

    [Fact]
    public void Read_ObsoleteEntries_Skipped()
    {
        var po =
            "msgid \"Keep me\"\n" +
            "msgstr \"Quédame\"\n" +
            "\n" +
            "#~ msgid \"Old message\"\n" +
            "#~ msgstr \"Mensaje viejo\"\n";

        var catalog = PoReader.Read(po);

        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("Keep me", entry.MsgId);
    }

    [Fact]
    public void Read_EmptyInput_EmptyCatalog()
    {
        var catalog = PoReader.Read("");

        Assert.Null(catalog.Header);
        Assert.Empty(catalog.Entries);
    }
}
