using System.Buffers.Binary;
using System.ComponentModel;

namespace TICSaveEditor.Core.Records;

/// <summary>
/// Read-only opaque wrapper over the 256-entry <c>battle.eventwork</c> region (1024 bytes,
/// 256 × int32 LE). Per spec §6.7, v0.1 exposes only <see cref="Get(int)"/>; mutation surface
/// is deferred until the meaning of individual event-work indices is documented.
/// </summary>
public class EventWork : INotifyPropertyChanged
{
    public const int Capacity = 0x100;
    public const int ByteLength = Capacity * sizeof(int);

    private readonly byte[] _bytes;

    internal EventWork(ReadOnlySpan<byte> source)
    {
        if (source.Length != ByteLength)
        {
            throw new ArgumentException(
                $"EventWork requires exactly {ByteLength} bytes (got {source.Length}).",
                nameof(source));
        }
        _bytes = source.ToArray();
    }

    public int Get(int index)
    {
        if ((uint)index >= (uint)Capacity)
            throw new ArgumentOutOfRangeException(nameof(index));
        return BinaryPrimitives.ReadInt32LittleEndian(_bytes.AsSpan(index * sizeof(int), sizeof(int)));
    }

    internal void WriteTo(Span<byte> destination)
    {
        if (destination.Length != ByteLength)
        {
            throw new ArgumentException(
                $"EventWork.WriteTo requires exactly {ByteLength} bytes (got {destination.Length}).",
                nameof(destination));
        }
        _bytes.AsSpan().CopyTo(destination);
    }

    /// <summary>
    /// Replaces the underlying byte storage from a snapshot. No INPC fires — EventWork's
    /// PropertyChanged is forward-compat scaffolding (v0.1 has no setter to drive it).
    /// Called during M8 BattleSection rollback.
    /// </summary>
    internal void RehydrateFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length != ByteLength)
        {
            throw new ArgumentException(
                $"EventWork.RehydrateFrom requires exactly {ByteLength} bytes (got {source.Length}).",
                nameof(source));
        }
        source.CopyTo(_bytes.AsSpan());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // Reserved for v0.2+ when individual indices gain semantic meaning.
    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
