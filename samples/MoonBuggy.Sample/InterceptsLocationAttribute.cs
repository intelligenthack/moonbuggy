// Polyfill for the InterceptsLocation attribute required by the MoonBuggy source generator.
// In a real project, the MoonBuggy NuGet package would provide this via the generator output.
#pragma warning disable CS9113
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptsLocationAttribute(int version, string data) : Attribute;
