using System.ComponentModel;
using System.Runtime.CompilerServices;
using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Save;

public abstract class SaveFile : INotifyPropertyChanged
{
    private readonly byte[]? _originalPngEnvelope;
    private readonly byte[]? _originalUnwrappedPayload;

    protected SaveFile(
        int version,
        uint storedChecksum,
        ulong formatDiscriminator,
        string sourcePath,
        byte[]? originalPngEnvelope,
        byte[]? originalUnwrappedPayload)
    {
        Version = version;
        StoredChecksum = storedChecksum;
        FormatDiscriminator = formatDiscriminator;
        SourcePath = sourcePath;
        _originalPngEnvelope = originalPngEnvelope;
        _originalUnwrappedPayload = originalUnwrappedPayload;
    }

    public int Version { get; }
    public uint StoredChecksum { get; protected set; }
    public ulong FormatDiscriminator { get; }
    public string SourcePath { get; protected set; }
    public abstract SaveFileKind Kind { get; }

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        protected set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged();
            }
        }
    }

    public abstract void Save();
    public abstract void SaveAs(string path);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected void WriteOutput(string path, byte[] payload)
    {
        byte[] output;
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && _originalPngEnvelope is not null)
        {
            // No-mutation fast path: if the current payload is byte-identical to what we
            // unwrapped from the source, write the original PNG envelope verbatim. This
            // preserves the byte-faithful round-trip invariant even though our deflater
            // (SharpZipLib) wouldn't produce a bit-exact UMIF blob versus Nenkai's source.
            if (_originalUnwrappedPayload is not null
                && payload.AsSpan().SequenceEqual(_originalUnwrappedPayload))
            {
                output = _originalPngEnvelope;
            }
            else
            {
                var umif = UmifContainer.Pack(payload);
                output = PngEnvelope.Repack(_originalPngEnvelope, umif);
            }
        }
        else
        {
            output = payload;
        }
        AtomicWrite.WriteAllBytes(path, output);
        SourcePath = path;
        IsDirty = false;
    }
}
