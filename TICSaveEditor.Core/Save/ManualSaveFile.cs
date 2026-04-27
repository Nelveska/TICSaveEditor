using System.Buffers.Binary;
using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Save;

public class ManualSaveFile : SaveFile, ISnapshotable, ISuspendable
{
    public const int SlotCount = 50;
    private const int OuterHeaderSize = 0x10;

    private int _suspendDepth;

    internal ManualSaveFile(
        int version,
        uint storedChecksum,
        ulong formatDiscriminator,
        string sourcePath,
        byte[]? originalPngEnvelope,
        byte[]? originalUnwrappedPayload,
        IReadOnlyList<SaveSlot> slots)
        : base(version, storedChecksum, formatDiscriminator, sourcePath, originalPngEnvelope, originalUnwrappedPayload)
    {
        if (slots.Count != SlotCount)
        {
            throw new ArgumentException(
                $"Manual saves must have exactly {SlotCount} slots.",
                nameof(slots));
        }
        Slots = slots;
    }

    public override SaveFileKind Kind => SaveFileKind.Manual;
    public IReadOnlyList<SaveSlot> Slots { get; }

    public override void Save() => SaveAs(SourcePath);

    public override void SaveAs(string path) => WriteOutput(path, BuildPayload());

    internal byte[] BuildPayload()
    {
        var totalSize = OuterHeaderSize + SlotCount * SaveWork.Size;
        var buffer = new byte[totalSize];

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x00, 4), (uint)Version);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(0x08, 8), FormatDiscriminator);

        for (var i = 0; i < SlotCount; i++)
        {
            Buffer.BlockCopy(
                Slots[i].SaveWork.RawBytes, 0,
                buffer, OuterHeaderSize + i * SaveWork.Size,
                SaveWork.Size);
        }

        var crc = Crc32.Compute(buffer.AsSpan(0x10));
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x04, 4), crc);
        StoredChecksum = crc;
        return buffer;
    }

    // ===== ISnapshotable =====

    public object CreateSnapshot() => BuildPayload();

    public void RestoreFromSnapshot(object snapshot)
    {
        if (snapshot is not byte[] bytes)
            throw new ArgumentException(
                $"ManualSaveFile.RestoreFromSnapshot expected byte[]; got {snapshot?.GetType().Name ?? "null"}.",
                nameof(snapshot));

        var expectedSize = OuterHeaderSize + SlotCount * SaveWork.Size;
        if (bytes.Length != expectedSize)
            throw new ArgumentException(
                $"ManualSaveFile snapshot must be exactly {expectedSize} bytes (got {bytes.Length}).",
                nameof(snapshot));

        // Each slot's SaveWork.RestoreFromSnapshot fires its own OnPropertyChanged(null).
        // ManualSaveFile-level INPC is inherited from SaveFile (currently only IsDirty drives it).
        var span = bytes.AsSpan();
        for (var i = 0; i < SlotCount; i++)
        {
            var slotBytes = span.Slice(OuterHeaderSize + i * SaveWork.Size, SaveWork.Size).ToArray();
            Slots[i].SaveWork.RestoreFromSnapshot(slotBytes);
        }
    }

    // ===== ISuspendable =====

    public IDisposable SuspendNotifications()
    {
        _suspendDepth++;
        var slotScopes = new IDisposable[SlotCount];
        for (var i = 0; i < SlotCount; i++)
        {
            slotScopes[i] = Slots[i].SaveWork.SuspendNotifications();
        }
        return new SuspendScope(this, slotScopes);
    }

    private sealed class SuspendScope : IDisposable
    {
        private readonly ManualSaveFile _owner;
        private readonly IDisposable[] _slotScopes;
        private bool _disposed;

        public SuspendScope(ManualSaveFile owner, IDisposable[] slotScopes)
        {
            _owner = owner;
            _slotScopes = slotScopes;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var scope in _slotScopes) scope.Dispose();
            _owner._suspendDepth--;
        }
    }
}
