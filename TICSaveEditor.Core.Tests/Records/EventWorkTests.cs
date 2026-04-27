using System.Buffers.Binary;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class EventWorkTests
{
    [Fact]
    public void Capacity_is_0x100()
    {
        Assert.Equal(0x100, EventWork.Capacity);
    }

    [Fact]
    public void ByteLength_is_1024()
    {
        Assert.Equal(1024, EventWork.ByteLength);
    }

    [Fact]
    public void Get_reads_int32_LE_at_index_zero()
    {
        var bytes = new byte[EventWork.ByteLength];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), 0x12345678);
        var ew = new EventWork(bytes);
        Assert.Equal(0x12345678, ew.Get(0));
    }

    [Fact]
    public void Get_reads_int32_LE_at_last_index()
    {
        var bytes = new byte[EventWork.ByteLength];
        BinaryPrimitives.WriteInt32LittleEndian(
            bytes.AsSpan((EventWork.Capacity - 1) * 4, 4), -42);
        var ew = new EventWork(bytes);
        Assert.Equal(-42, ew.Get(EventWork.Capacity - 1));
    }

    [Fact]
    public void Get_throws_for_negative_index()
    {
        var ew = new EventWork(new byte[EventWork.ByteLength]);
        Assert.Throws<ArgumentOutOfRangeException>(() => ew.Get(-1));
    }

    [Fact]
    public void Get_throws_for_index_at_capacity()
    {
        var ew = new EventWork(new byte[EventWork.ByteLength]);
        Assert.Throws<ArgumentOutOfRangeException>(() => ew.Get(EventWork.Capacity));
    }

    [Fact]
    public void Construction_throws_on_wrong_length()
    {
        Assert.Throws<ArgumentException>(() => new EventWork(new byte[1023]));
        Assert.Throws<ArgumentException>(() => new EventWork(new byte[1025]));
    }

    [Fact]
    public void WriteTo_round_trips_construction_bytes_byte_identical()
    {
        var rng = new Random(11);
        var source = new byte[EventWork.ByteLength];
        rng.NextBytes(source);

        var ew = new EventWork(source);
        var output = new byte[EventWork.ByteLength];
        ew.WriteTo(output);
        Assert.Equal(source, output);
    }
}
