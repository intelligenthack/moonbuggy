using System.IO;
using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using MoonBuggy;

namespace MoonBuggy.Benchmarks;

/// <summary>
/// Compares the zero-alloc WriteTo path against ToString/implicit conversion
/// which must allocate a string.
/// </summary>
[MemoryDiagnoser]
public class ToStringBenchmarks
{
    private StringWriter _writer = null!;
    private HtmlEncoder _encoder = null!;

    private TranslatedString _singleSegment;
    private TranslatedString _multiSegment;

    [GlobalSetup]
    public void Setup()
    {
        _writer = new StringWriter();
        _encoder = HtmlEncoder.Default;

        _singleSegment = new TranslatedString("Hello, World!");
        _multiSegment = new TranslatedString(
            new string?[] { "Hello, ", "Alice", "!" },
            new bool[] { false, true, false });
    }

    [IterationSetup]
    public void ResetWriter()
    {
        _writer.GetStringBuilder().Clear();
    }

    [Benchmark(Baseline = true)]
    public void WriteTo_SingleSegment()
    {
        _singleSegment.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public string ToString_SingleSegment()
    {
        return _singleSegment.ToString();
    }

    [Benchmark]
    public void WriteTo_MultiSegment()
    {
        _multiSegment.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public string ToString_MultiSegment()
    {
        return _multiSegment.ToString();
    }

    [Benchmark]
    public string ImplicitConversion()
    {
        string s = _multiSegment;
        return s;
    }
}
