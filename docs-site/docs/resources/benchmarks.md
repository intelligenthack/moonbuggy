---
sidebar_position: 1
title: Benchmarks
---

# MoonBuggy Microbenchmarks

Benchmarks run with BenchmarkDotNet v0.14.0 on:

- **OS**: Ubuntu 24.04.3 LTS (WSL)
- **CPU**: 12th Gen Intel Core i9-12900K, 1 CPU, 24 logical / 12 physical cores
- **Runtime**: .NET 8.0.23 (RyuJIT AVX2)

## WriteTo Performance

Measures `IHtmlContent.WriteTo()` overhead for `TranslatedString` and `TranslatedHtml` against raw `TextWriter.Write()` baselines.

| Method | Mean | Error | StdDev | Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| StringBaseline_Write | 299.6 ns | 30.34 ns | 81.52 ns | 301.0 ns | 1.09 | 0.47 | 736 B | 1.00 |
| HtmlStringBaseline_WriteTo | 466.5 ns | 26.96 ns | 71.96 ns | 458.5 ns | 1.70 | 0.62 | 736 B | 1.00 |
| TranslatedString_SingleSegment | 474.9 ns | 57.26 ns | 165.21 ns | 428.5 ns | 1.73 | 0.84 | 736 B | 1.00 |
| TranslatedString_MultiSegment | 1,208.3 ns | 76.71 ns | 223.78 ns | 1,137.0 ns | 4.40 | 1.67 | 736 B | 1.00 |
| TranslatedHtml_SingleSegment | 472.0 ns | 47.51 ns | 133.22 ns | 436.0 ns | 1.72 | 0.76 | 736 B | 1.00 |
| TranslatedHtml_MultiSegment | 497.7 ns | 24.24 ns | 67.57 ns | 498.0 ns | 1.81 | 0.64 | 64 B | 0.09 |

**Key findings:**
- Single-segment `TranslatedString` and `TranslatedHtml` match the `HtmlString.WriteTo()` baseline (~470 ns).
- Multi-segment `TranslatedHtml` allocates only 64 B (0.09x ratio) — the pre-rendered HTML path avoids per-segment allocations.
- All 736 B allocations come from the `StringWriter` infrastructure itself, not from MoonBuggy.

## ToString vs WriteTo

Compares `ToString()` (allocates a string) against `WriteTo()` (writes directly to `TextWriter`).

| Method | Mean | Error | StdDev | Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| WriteTo_SingleSegment | 379.88 ns | 22.87 ns | 62.22 ns | 370.50 ns | 1.03 | 0.23 | 736 B | 1.00 |
| ToString_SingleSegment | 36.11 ns | 14.12 ns | 41.64 ns | 19.50 ns | 0.10 | 0.11 | 736 B | 1.00 |
| WriteTo_MultiSegment | 1,054.43 ns | 67.82 ns | 194.58 ns | 987.50 ns | 2.85 | 0.69 | 736 B | 1.00 |
| ToString_MultiSegment | 853.65 ns | 60.38 ns | 166.30 ns | 810.00 ns | 2.30 | 0.57 | 784 B | 1.07 |
| ImplicitConversion | 876.63 ns | 43.93 ns | 119.51 ns | 877.50 ns | 2.37 | 0.49 | 784 B | 1.07 |

**Key findings:**
- Single-segment `ToString()` is ~10x faster than `WriteTo()` — it returns the pre-stored string directly without `TextWriter` overhead.
- Multi-segment `ToString()` allocates 784 B (1.07x) due to the intermediate string concatenation.
- Implicit `string` conversion matches `ToString()` performance as expected.

## Interceptor Simulation

Simulates the code patterns emitted by the source generator interceptors, measuring the cost of locale lookup, variable substitution, and plural resolution.

| Method | Mean | Error | StdDev | Median | Allocated |
|---|---:|---:|---:|---:|---:|
| SourceLocale_Simple | 591.4 ns | 62.26 ns | 182.6 ns | 522.5 ns | 736 B |
| SourceLocale_Variable | 1,734.6 ns | 113.30 ns | 330.5 ns | 1,672.5 ns | 816 B |
| SourceLocale_Plural_One | 1,990.4 ns | 102.58 ns | 287.6 ns | 1,926.0 ns | 816 B |
| SourceLocale_Plural_Other | 1,959.9 ns | 101.23 ns | 290.5 ns | 1,868.5 ns | 816 B |
| MultiLocale_Simple | 1,386.1 ns | 90.93 ns | 265.3 ns | 1,322.0 ns | 736 B |
| MultiLocale_Variable | 2,587.1 ns | 170.23 ns | 491.2 ns | 2,437.0 ns | 816 B |

**Key findings:**
- Source-locale simple translation: ~590 ns with zero extra allocations beyond the `StringWriter`.
- Variable substitution adds ~1.1 µs per call due to additional `Write()` calls and argument access.
- Plural resolution adds minimal overhead over variable substitution (~250 ns).
- Multi-locale lookup (LCID switch) adds ~800 ns over the source-locale path.
- The 80 B difference (736 → 816 B) in variable/plural benchmarks comes from the args object allocation.

## Running Benchmarks

```bash
# All benchmarks
dotnet run --project tests/MoonBuggy.Benchmarks -c Release -- --filter '*'

# Individual suites
dotnet run --project tests/MoonBuggy.Benchmarks -c Release -- --filter '*WriteTo*'
dotnet run --project tests/MoonBuggy.Benchmarks -c Release -- --filter '*ToString*'
dotnet run --project tests/MoonBuggy.Benchmarks -c Release -- --filter '*Interceptor*'
```

Results are saved to `BenchmarkDotNet.Artifacts/` in the project root.
