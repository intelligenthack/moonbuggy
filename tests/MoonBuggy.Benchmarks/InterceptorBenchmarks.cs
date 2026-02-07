using System.IO;
using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using MoonBuggy;

namespace MoonBuggy.Benchmarks;

/// <summary>
/// Simulates the code patterns emitted by the source generator.
/// Each benchmark replicates what an interceptor body does at runtime,
/// using pre-extracted typed values (the generator resolves args at compile time).
/// </summary>
[MemoryDiagnoser]
public class InterceptorBenchmarks
{
    private StringWriter _writer = null!;
    private HtmlEncoder _encoder = null!;
    private string _name = null!;
    private int _count1;
    private int _count5;

    [GlobalSetup]
    public void Setup()
    {
        _writer = new StringWriter();
        _encoder = HtmlEncoder.Default;
        _name = "Alice";
        _count1 = 1;
        _count5 = 5;

        // Set locale to English (LCID 9)
        I18n.Current = new I18nContext { LCID = 9 };
    }

    [IterationSetup]
    public void ResetWriter()
    {
        _writer.GetStringBuilder().Clear();
    }

    // --- Source locale (no LCID check) ---

    [Benchmark]
    public void SourceLocale_Simple()
    {
        // Simulates: return new TranslatedString("Save changes");
        var result = new TranslatedString("Save changes");
        result.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void SourceLocale_Variable()
    {
        // Simulates: return new TranslatedString(new[] { "Hello, ", name, "!" }, ...)
        var result = new TranslatedString(
            new string?[] { "Hello, ", _name, "!" },
            new bool[] { false, true, false });
        result.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void SourceLocale_Plural_One()
    {
        // Simulates: plural with count=1 → "one" branch
        int count = _count1;

        TranslatedString result;
        if (count == 1)
            result = new TranslatedString(
                new string?[] { "", count.ToString(), " item" },
                new bool[] { false, true, false });
        else
            result = new TranslatedString(
                new string?[] { "", count.ToString(), " items" },
                new bool[] { false, true, false });

        result.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void SourceLocale_Plural_Other()
    {
        // Simulates: plural with count=5 → "other" branch
        int count = _count5;

        TranslatedString result;
        if (count == 1)
            result = new TranslatedString(
                new string?[] { "", count.ToString(), " item" },
                new bool[] { false, true, false });
        else
            result = new TranslatedString(
                new string?[] { "", count.ToString(), " items" },
                new bool[] { false, true, false });

        result.WriteTo(_writer, _encoder);
    }

    // --- Multi-locale (LCID switch) ---

    [Benchmark]
    public void MultiLocale_Simple()
    {
        // Simulates: LCID check for en(9)/es(10)/fr(12), return per-locale string
        var lcid = I18n.Current.LCID;
        TranslatedString result;
        if (lcid == 10) // es
            result = new TranslatedString("Guardar cambios");
        else if (lcid == 12) // fr
            result = new TranslatedString("Enregistrer les modifications");
        else
            result = new TranslatedString("Save changes");

        result.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void MultiLocale_Variable()
    {
        // Simulates: LCID check + variable per locale
        var lcid = I18n.Current.LCID;

        TranslatedString result;
        if (lcid == 10) // es
            result = new TranslatedString(
                new string?[] { "Hola, ", _name, "!" },
                new bool[] { false, true, false });
        else if (lcid == 12) // fr
            result = new TranslatedString(
                new string?[] { "Bonjour, ", _name, " !" },
                new bool[] { false, true, false });
        else
            result = new TranslatedString(
                new string?[] { "Hello, ", _name, "!" },
                new bool[] { false, true, false });

        result.WriteTo(_writer, _encoder);
    }
}
