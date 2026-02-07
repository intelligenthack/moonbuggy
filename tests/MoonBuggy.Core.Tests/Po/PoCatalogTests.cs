using MoonBuggy.Core.Po;

namespace MoonBuggy.Core.Tests.Po;

public class PoCatalogTests
{
    [Fact]
    public void Find_ExistingEntry_ReturnsIt()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Hello", MsgStr = "Hola" });

        var found = catalog.Find("Hello");

        Assert.NotNull(found);
        Assert.Equal("Hola", found!.MsgStr);
    }

    [Fact]
    public void Find_Missing_ReturnsNull()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Hello", MsgStr = "Hola" });

        var found = catalog.Find("Goodbye");

        Assert.Null(found);
    }

    [Fact]
    public void Find_WithContext_MatchesCorrectEntry()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Submit", MsgStr = "Enviar", MsgCtxt = "button" });
        catalog.Entries.Add(new PoEntry { MsgId = "Submit", MsgStr = "Presentar", MsgCtxt = "legal" });

        var button = catalog.Find("Submit", "button");
        var legal = catalog.Find("Submit", "legal");
        var noCtx = catalog.Find("Submit");

        Assert.Equal("Enviar", button!.MsgStr);
        Assert.Equal("Presentar", legal!.MsgStr);
        Assert.Null(noCtx);
    }

    [Fact]
    public void GetOrAdd_NewEntry_CreatesWithEmptyMsgStr()
    {
        var catalog = new PoCatalog();

        var entry = catalog.GetOrAdd("Save changes");

        Assert.Equal("Save changes", entry.MsgId);
        Assert.Equal("", entry.MsgStr);
        Assert.Single(catalog.Entries);
    }

    [Fact]
    public void GetOrAdd_ExistingEntry_PreservesMsgStr()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Save changes", MsgStr = "Guardar cambios" });

        var entry = catalog.GetOrAdd("Save changes");

        Assert.Equal("Guardar cambios", entry.MsgStr);
        Assert.Single(catalog.Entries);
    }

    [Fact]
    public void RemoveObsolete_RemovesEntriesNotInActiveSet()
    {
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Keep me", MsgStr = "Quédame" });
        catalog.Entries.Add(new PoEntry { MsgId = "Old message", MsgStr = "Mensaje viejo" });

        var activeKeys = new HashSet<(string, string?)> { ("Keep me", null) };
        var removed = catalog.RemoveObsolete(activeKeys);

        Assert.Equal(1, removed);
        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("Keep me", entry.MsgId);
    }

    // Merge scenario: spec 8.1 — new entry into empty catalog
    [Fact]
    public void Merge_NewEntryIntoEmptyCatalog_EmptyMsgStr()
    {
        var catalog = new PoCatalog();

        // Simulate extract: source has _t("Save changes")
        var entry = catalog.GetOrAdd("Save changes");

        Assert.Equal("Save changes", entry.MsgId);
        Assert.Equal("", entry.MsgStr);
    }

    // Merge scenario: spec 8.2 + 8.3 — re-extract preserves, then clean removes obsolete
    [Fact]
    public void Merge_ReExtractPreservesTranslation_ThenCleanRemovesObsolete()
    {
        // Start with an existing catalog (from PO file)
        var catalog = new PoCatalog();
        catalog.Entries.Add(new PoEntry { MsgId = "Save changes", MsgStr = "Guardar cambios" });
        catalog.Entries.Add(new PoEntry { MsgId = "Old message", MsgStr = "Mensaje viejo" });

        // Re-extract: source only has _t("Save changes") and _t("New message")
        var save = catalog.GetOrAdd("Save changes");
        var newMsg = catalog.GetOrAdd("New message");

        // 8.2: existing translation preserved
        Assert.Equal("Guardar cambios", save.MsgStr);
        // 8.1: new entry has empty msgstr
        Assert.Equal("", newMsg.MsgStr);

        // 8.3: clean removes "Old message"
        var activeKeys = new HashSet<(string, string?)>
        {
            ("Save changes", null),
            ("New message", null)
        };
        var removed = catalog.RemoveObsolete(activeKeys);

        Assert.Equal(1, removed);
        Assert.Equal(2, catalog.Entries.Count);
        Assert.NotNull(catalog.Find("Save changes"));
        Assert.NotNull(catalog.Find("New message"));
        Assert.Null(catalog.Find("Old message"));
    }
}
