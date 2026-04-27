// Adapted from Nenkai/FF16Tools (MIT) — see THIRD_PARTY_LICENSES.md.
//
// The 32 KB zlib preset dictionary used by UMIF-wrapped saves. Stored as an
// embedded binary resource (Resources/CompressDict.bin) rather than a C#
// byte-array literal — the literal expands to ~200 KB of source for no
// runtime benefit. Loaded once on first use, cached for process lifetime.

namespace TICSaveEditor.Core.Save;

internal static class UmifCompressDict
{
    public const int Length = 0x8000;
    private const string ResourceName = "TICSaveEditor.Core.Resources.CompressDict.bin";

    private static byte[]? _cached;

    public static byte[] Bytes => _cached ??= Load();

    private static byte[] Load()
    {
        using var stream = typeof(UmifCompressDict).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {ResourceName}");
        var buffer = new byte[Length];
        stream.ReadExactly(buffer);
        return buffer;
    }
}
