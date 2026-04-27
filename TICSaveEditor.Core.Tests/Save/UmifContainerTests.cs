using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

public class UmifContainerTests
{
    private static readonly string[] FixtureNames =
    {
        "Baseline", "EquipSet", "InternalChecksum", "Inventory", "JobChange",
    };

    private static byte[] ReadFixtureFftoChunk(string name)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", name, "enhanced.png");
        var png = File.ReadAllBytes(path);
        return PngEnvelope.Extract(png);
    }

    [Fact]
    public void Crypt_is_self_inverse()
    {
        var rng = new Random(2026);
        var data = new byte[1024];
        rng.NextBytes(data);
        var pristine = data.ToArray();

        UmifContainer.Crypt(data);
        UmifContainer.Crypt(data);

        Assert.Equal(pristine, data);
    }

    [Fact]
    public void Crypt_handles_lengths_below_8_bytes()
    {
        // Each tail-size branch (4, 2, 1) must round-trip too.
        foreach (var len in new[] { 1, 2, 3, 4, 5, 7, 8, 13, 17 })
        {
            var rng = new Random(len);
            var data = new byte[len];
            rng.NextBytes(data);
            var pristine = data.ToArray();
            UmifContainer.Crypt(data);
            UmifContainer.Crypt(data);
            Assert.Equal(pristine, data);
        }
    }

    [Fact]
    public void Unpack_throws_on_short_blob()
    {
        Assert.Throws<InvalidDataException>(() => UmifContainer.Unpack(new byte[8]));
    }

    [Fact]
    public void Unpack_throws_on_bad_magic()
    {
        var bytes = new byte[0x40];
        // No UMIF magic at 0x08.
        Assert.Throws<InvalidDataException>(() => UmifContainer.Unpack(bytes));
    }

    [Theory]
    [InlineData("Baseline")]
    [InlineData("EquipSet")]
    [InlineData("InternalChecksum")]
    [InlineData("Inventory")]
    [InlineData("JobChange")]
    public void Unpack_real_fixture_returns_2007816_bytes(string fixture)
    {
        var chunk = ReadFixtureFftoChunk(fixture);
        var payload = UmifContainer.Unpack(chunk);
        Assert.Equal(2_007_816, payload.Length);
    }

    [Fact]
    public void Unpack_real_fixture_payload_starts_with_outer_header()
    {
        var chunk = ReadFixtureFftoChunk("Baseline");
        var payload = UmifContainer.Unpack(chunk);
        // First 4 bytes: version (uint32). Should be a small integer, not a UMIF magic.
        Assert.NotEqual(0x46494D55u, BitConverter.ToUInt32(payload, 0));
    }

    [Fact]
    public void Pack_then_Unpack_round_trips_byte_identical_for_random_payload()
    {
        var rng = new Random(2026);
        var payload = new byte[2_007_816];
        rng.NextBytes(payload);

        var umif = UmifContainer.Pack(payload, "fftsave.bin");
        var unpacked = UmifContainer.Unpack(umif);

        // Pack mutates bytes 0x04..0x07 of the input via CRC32 fix-up.
        // Compare against the post-fix-up form by re-computing the same CRC.
        var expected = (byte[])payload.Clone();
        var crc = TICSaveEditor.Core.Util.Crc32.Compute(expected.AsSpan(0x10));
        expected[0x04] = (byte)crc;
        expected[0x05] = (byte)(crc >> 8);
        expected[0x06] = (byte)(crc >> 16);
        expected[0x07] = (byte)(crc >> 24);

        Assert.Equal(expected, unpacked);
    }

    [Fact]
    public void Pack_then_Unpack_round_trips_real_fixture_payload()
    {
        // Unpack a real save's payload, then Pack and Unpack it again.
        // The result must equal the first unpacked payload.
        var chunk = ReadFixtureFftoChunk("Baseline");
        var firstUnpacked = UmifContainer.Unpack(chunk);

        var repacked = UmifContainer.Pack(firstUnpacked);
        var secondUnpacked = UmifContainer.Unpack(repacked);

        Assert.Equal(firstUnpacked, secondUnpacked);
    }

    [Fact]
    public void Pack_produces_UMIF_magic_at_offset_0x08()
    {
        var payload = new byte[2_007_816];
        var umif = UmifContainer.Pack(payload);
        Assert.Equal(0x55, umif[0x08]); // 'U'
        Assert.Equal(0x4D, umif[0x09]); // 'M'
        Assert.Equal(0x49, umif[0x0A]); // 'I'
        Assert.Equal(0x46, umif[0x0B]); // 'F'
    }

    [Fact]
    public void Pack_compressed_size_is_much_smaller_than_payload()
    {
        // Sanity: 2 MB of zeros should compress to a few hundred bytes
        // (zlib + dictionary makes near-empty data tiny).
        var payload = new byte[2_007_816];
        var umif = UmifContainer.Pack(payload);
        Assert.InRange(umif.Length, 200, 5_000);
    }
}
