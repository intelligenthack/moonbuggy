using MoonBuggy.Core.Po;

namespace MoonBuggy.Core.Tests.Po;

public class PoEntryTests
{
    [Fact]
    public void DefaultValues_AreEmptyStringsNullAndEmptyLists()
    {
        var entry = new PoEntry();

        Assert.Equal("", entry.MsgId);
        Assert.Equal("", entry.MsgStr);
        Assert.Null(entry.MsgCtxt);
        Assert.Empty(entry.TranslatorComments);
        Assert.Empty(entry.ExtractedComments);
        Assert.Empty(entry.References);
        Assert.Empty(entry.Flags);
    }

    [Fact]
    public void SetAndReadBack_AllProperties()
    {
        var entry = new PoEntry
        {
            MsgId = "Save changes",
            MsgStr = "Guardar cambios",
            MsgCtxt = "button"
        };
        entry.TranslatorComments.Add("This is a button label");
        entry.ExtractedComments.Add("src/Views/Edit.cshtml:42");
        entry.References.Add("src/Views/Edit.cshtml:42");
        entry.Flags.Add("fuzzy");

        Assert.Equal("Save changes", entry.MsgId);
        Assert.Equal("Guardar cambios", entry.MsgStr);
        Assert.Equal("button", entry.MsgCtxt);
        Assert.Single(entry.TranslatorComments, "This is a button label");
        Assert.Single(entry.ExtractedComments, "src/Views/Edit.cshtml:42");
        Assert.Single(entry.References, "src/Views/Edit.cshtml:42");
        Assert.Single(entry.Flags, "fuzzy");
    }
}
