using MoonBuggy.Core.Po;

namespace MoonBuggy.Tests;

public class PoRoundTripTests
{
    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var catalog = new PoCatalog();
        var entry = new PoEntry
        {
            MsgId = "Hello",
            MsgStr = "Hola",
            MsgCtxt = "greeting"
        };
        entry.TranslatorComments.Add("Used on login page");
        entry.ExtractedComments.Add("auto");
        entry.References.Add("src/Views/Login.cshtml:5");
        entry.Flags.Add("fuzzy");
        catalog.Entries.Add(entry);

        var po = PoWriter.Write(catalog);
        var roundTripped = PoReader.Read(po);

        var rt = Assert.Single(roundTripped.Entries);
        Assert.Equal("Hello", rt.MsgId);
        Assert.Equal("Hola", rt.MsgStr);
        Assert.Equal("greeting", rt.MsgCtxt);
        Assert.Single(rt.TranslatorComments, "Used on login page");
        Assert.Single(rt.ExtractedComments, "auto");
        Assert.Single(rt.References, "src/Views/Login.cshtml:5");
        Assert.Single(rt.Flags, "fuzzy");
    }

    [Fact]
    public void RoundTrip_IcuMessageFormatStrings()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry
        {
            MsgId = "You have {x, plural, one {# book} other {# books}}",
            MsgStr = "Tienes {x, plural, one {# libro} other {# libros}}"
        });

        var po = PoWriter.Write(catalog);
        var roundTripped = PoReader.Read(po);

        var rt = Assert.Single(roundTripped.Entries);
        Assert.Equal("You have {x, plural, one {# book} other {# books}}", rt.MsgId);
        Assert.Equal("Tienes {x, plural, one {# libro} other {# libros}}", rt.MsgStr);
    }

    [Fact]
    public void RoundTrip_HeaderWithMultilineMsgStr()
    {
        var catalog = new PoCatalog
        {
            Header = new PoEntry
            {
                MsgId = "",
                MsgStr = "Content-Type: text/plain; charset=UTF-8\nPlural-Forms: nplurals=2; plural=(n != 1);\n"
            }
        };
        catalog.Entries.Add(new PoEntry { MsgId = "Hello", MsgStr = "Hola" });

        var po = PoWriter.Write(catalog);
        var roundTripped = PoReader.Read(po);

        Assert.NotNull(roundTripped.Header);
        Assert.Equal(
            "Content-Type: text/plain; charset=UTF-8\nPlural-Forms: nplurals=2; plural=(n != 1);\n",
            roundTripped.Header!.MsgStr);
        var entry = Assert.Single(roundTripped.Entries);
        Assert.Equal("Hello", entry.MsgId);
    }
}
