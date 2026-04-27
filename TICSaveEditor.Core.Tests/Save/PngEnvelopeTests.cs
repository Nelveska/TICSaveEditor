using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Tests.Save;

public class PngEnvelopeTests
{
    [Fact]
    public void Extract_returns_ffTo_payload_verbatim()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var png = BuildMinimalPngWithFfto(payload);

        var extracted = PngEnvelope.Extract(png);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public void Repack_replaces_ffTo_payload()
    {
        var oldPayload = new byte[] { 0xAA, 0xBB, 0xCC };
        var newPayload = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 };
        var png = BuildMinimalPngWithFfto(oldPayload);

        var repacked = PngEnvelope.Repack(png, newPayload);
        var extracted = PngEnvelope.Extract(repacked);

        Assert.Equal(newPayload, extracted);
    }

    [Fact]
    public void Repack_preserves_other_chunks_verbatim()
    {
        var oldPayload = new byte[] { 0x01, 0x02 };
        var newPayload = new byte[] { 0xFF, 0xEE, 0xDD };
        var png = BuildMinimalPngWithFfto(oldPayload);

        var repacked = PngEnvelope.Repack(png, newPayload);

        Assert.True(repacked.AsSpan(0, 8).SequenceEqual(png.AsSpan(0, 8)));
        var ihdrLen = 4 + 4 + 13 + 4;
        Assert.True(repacked.AsSpan(8, ihdrLen).SequenceEqual(png.AsSpan(8, ihdrLen)));
        var iendLen = 4 + 4 + 0 + 4;
        Assert.True(repacked.AsSpan(repacked.Length - iendLen, iendLen)
            .SequenceEqual(png.AsSpan(png.Length - iendLen, iendLen)));
    }

    [Fact]
    public void Extract_throws_on_missing_ffTo()
    {
        var png = BuildMinimalPngWithoutFfto();
        Assert.Throws<InvalidDataException>(() => PngEnvelope.Extract(png));
    }

    [Fact]
    public void Extract_throws_on_bad_signature()
    {
        var notPng = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        Assert.Throws<InvalidDataException>(() => PngEnvelope.Extract(notPng));
    }

    private static byte[] BuildMinimalPngWithFfto(byte[] payload)
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        WriteChunk(ms, "IHDR", new byte[13]);
        WriteChunk(ms, "ffTo", payload);
        WriteChunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static byte[] BuildMinimalPngWithoutFfto()
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        WriteChunk(ms, "IHDR", new byte[13]);
        WriteChunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static void WriteChunk(Stream s, string type, byte[] data)
    {
        Span<byte> lengthBytes = stackalloc byte[4];
        var len = (uint)data.Length;
        lengthBytes[0] = (byte)(len >> 24);
        lengthBytes[1] = (byte)(len >> 16);
        lengthBytes[2] = (byte)(len >> 8);
        lengthBytes[3] = (byte)len;
        s.Write(lengthBytes);

        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes);

        s.Write(data);

        var crcInput = new byte[4 + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcInput, 0, 4);
        Buffer.BlockCopy(data, 0, crcInput, 4, data.Length);
        var crc = Crc32.Compute(crcInput);

        Span<byte> crcBytes = stackalloc byte[4];
        crcBytes[0] = (byte)(crc >> 24);
        crcBytes[1] = (byte)(crc >> 16);
        crcBytes[2] = (byte)(crc >> 8);
        crcBytes[3] = (byte)crc;
        s.Write(crcBytes);
    }
}
