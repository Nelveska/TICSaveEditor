using System.Buffers.Binary;
using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Save;

public class ResumeWorldSaveFile : SaveFile
{
    internal ResumeWorldSaveFile(
        int version,
        uint storedChecksum,
        ulong formatDiscriminator,
        string sourcePath,
        byte[]? originalPngEnvelope,
        byte[]? originalUnwrappedPayload,
        FftiHeader fftiHeader,
        SaveWork saveWork)
        : base(version, storedChecksum, formatDiscriminator, sourcePath, originalPngEnvelope, originalUnwrappedPayload)
    {
        FftiHeader = fftiHeader;
        SaveWork = saveWork;
    }

    public override SaveFileKind Kind => SaveFileKind.ResumeWorld;
    public FftiHeader FftiHeader { get; }
    public SaveWork SaveWork { get; }

    public override void Save() => SaveAs(SourcePath);

    public override void SaveAs(string path) => WriteOutput(path, BuildPayload());

    private byte[] BuildPayload()
    {
        var fftiBytes = FftiHeader.RawBytes;
        var totalSize = 0x10 + fftiBytes.Length + SaveWork.Size;
        var buffer = new byte[totalSize];

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x00, 4), (uint)Version);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(0x08, 8), FormatDiscriminator);
        Buffer.BlockCopy(fftiBytes, 0, buffer, 0x10, fftiBytes.Length);
        Buffer.BlockCopy(
            SaveWork.RawBytes, 0,
            buffer, 0x10 + fftiBytes.Length,
            SaveWork.Size);

        var crc = Crc32.Compute(buffer.AsSpan(0x10));
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x04, 4), crc);
        StoredChecksum = crc;
        return buffer;
    }
}
