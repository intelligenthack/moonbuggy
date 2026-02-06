// Polyfill for record types on netstandard2.0
// The compiler needs this type to support init-only setters and positional records.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
