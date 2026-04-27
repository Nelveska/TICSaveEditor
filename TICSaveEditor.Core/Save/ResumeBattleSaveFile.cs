namespace TICSaveEditor.Core.Save;

public class ResumeBattleSaveFile : SaveFile
{
    private const string NotEditableMessage =
        "In-battle save files are not editable in this version.";

    internal ResumeBattleSaveFile(
        int version,
        uint storedChecksum,
        ulong formatDiscriminator,
        string sourcePath,
        byte[]? originalPngEnvelope,
        byte[]? originalUnwrappedPayload,
        FftiHeader fftiHeader,
        byte[] rawPayload)
        : base(version, storedChecksum, formatDiscriminator, sourcePath, originalPngEnvelope, originalUnwrappedPayload)
    {
        FftiHeader = fftiHeader;
        RawPayload = rawPayload;
    }

    public override SaveFileKind Kind => SaveFileKind.ResumeBattle;
    public FftiHeader FftiHeader { get; }
    public byte[] RawPayload { get; }

    public override void Save() => throw new NotSupportedException(NotEditableMessage);
    public override void SaveAs(string path) => throw new NotSupportedException(NotEditableMessage);
}
