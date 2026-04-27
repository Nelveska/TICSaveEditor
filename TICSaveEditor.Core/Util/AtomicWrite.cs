namespace TICSaveEditor.Core.Util;

internal static class AtomicWrite
{
    public static void WriteAllBytes(string path, ReadOnlySpan<byte> bytes)
    {
        var temp = path + ".tmp";

        using (var stream = new FileStream(
            temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }

        File.Move(temp, path, overwrite: true);
    }
}
