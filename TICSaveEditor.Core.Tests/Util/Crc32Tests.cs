using System.Text;
using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Tests.Util;

public class Crc32Tests
{
    [Fact]
    public void Empty_input_is_zero()
    {
        Assert.Equal(0u, Crc32.Compute(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Standard_check_vector_123456789()
    {
        var bytes = Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(0xCBF43926u, Crc32.Compute(bytes));
    }

    [Fact]
    public void Single_byte_a()
    {
        var bytes = Encoding.ASCII.GetBytes("a");
        Assert.Equal(0xE8B7BE43u, Crc32.Compute(bytes));
    }

    [Fact]
    public void Three_byte_abc()
    {
        var bytes = Encoding.ASCII.GetBytes("abc");
        Assert.Equal(0x352441C2u, Crc32.Compute(bytes));
    }

    [Fact]
    public void Compute_is_deterministic()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0xFF };
        var first = Crc32.Compute(bytes);
        var second = Crc32.Compute(bytes);
        Assert.Equal(first, second);
    }
}
