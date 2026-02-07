using System.IO;
using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Html;
using MoonBuggy;

namespace MoonBuggy.Benchmarks;

[MemoryDiagnoser]
public class WriteToBenchmarks
{
    private StringWriter _writer = null!;
    private HtmlEncoder _encoder = null!;

    private TranslatedString _singleSegmentString;
    private TranslatedString _multiSegmentString;
    private TranslatedHtml _singleSegmentHtml = null!;
    private TranslatedHtml _multiSegmentHtml = null!;
    private HtmlString _htmlStringBaseline = null!;

    [GlobalSetup]
    public void Setup()
    {
        _writer = new StringWriter();
        _encoder = HtmlEncoder.Default;

        _singleSegmentString = new TranslatedString("Hello, World!");
        _multiSegmentString = new TranslatedString(
            new string?[] { "Hello, ", "Alice", "!" },
            new bool[] { false, true, false });

        _singleSegmentHtml = new TranslatedHtml("<p>Hello, World!</p>");
        _multiSegmentHtml = new TranslatedHtml(
            new string?[] { "<p>Hello, ", "<b>Alice</b>", "!</p>" });

        _htmlStringBaseline = new HtmlString("<p>Hello, World!</p>");
    }

    [IterationSetup]
    public void ResetWriter()
    {
        _writer.GetStringBuilder().Clear();
    }

    [Benchmark(Baseline = true)]
    public void StringBaseline_Write()
    {
        _writer.Write("Hello, World!");
    }

    [Benchmark]
    public void HtmlStringBaseline_WriteTo()
    {
        _htmlStringBaseline.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void TranslatedString_SingleSegment()
    {
        _singleSegmentString.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void TranslatedString_MultiSegment()
    {
        _multiSegmentString.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void TranslatedHtml_SingleSegment()
    {
        _singleSegmentHtml.WriteTo(_writer, _encoder);
    }

    [Benchmark]
    public void TranslatedHtml_MultiSegment()
    {
        _multiSegmentHtml.WriteTo(_writer, _encoder);
    }
}
