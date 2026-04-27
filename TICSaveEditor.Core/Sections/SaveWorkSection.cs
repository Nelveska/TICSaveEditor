using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Sections;

public abstract class SaveWorkSection : INotifyPropertyChanged
{
    protected byte[] Bytes { get; }

    private int _suspendDepth;
    private readonly HashSet<string> _queuedEvents = new();

    internal SaveWorkSection(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException(
                $"{GetType().Name} requires exactly {Size} bytes (got {bytes.Length}).",
                nameof(bytes));
        }
        Bytes = bytes.ToArray();
    }

    internal abstract int Size { get; }

    internal virtual void WriteTo(Span<byte> destination)
    {
        if (destination.Length != Size)
        {
            throw new ArgumentException(
                $"{GetType().Name}.WriteTo requires exactly {Size} bytes (got {destination.Length}).",
                nameof(destination));
        }
        Bytes.CopyTo(destination);
    }

    /// <summary>
    /// Replaces the section's underlying state from a snapshot byte slice and fires
    /// <c>OnPropertyChanged(null)</c> to notify subscribers that everything may have changed.
    /// Called by <see cref="Save.SaveWork.RestoreFromSnapshot"/> during rollback.
    /// </summary>
    internal virtual void RehydrateFrom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException(
                $"{GetType().Name}.RehydrateFrom requires exactly {Size} bytes (got {bytes.Length}).",
                nameof(bytes));
        }
        bytes.CopyTo(Bytes.AsSpan());
        OnPropertyChanged(null);
    }

    /// <summary>
    /// Opens a suspend scope. While the scope is active, <see cref="OnPropertyChanged"/>
    /// queues each unique property name into a HashSet instead of firing immediately.
    /// On dispose at depth 0, queued events fire once each. Nested calls compose via
    /// counter (mirrors <c>UnitSaveData.SuspendNotifications</c>, M4 pattern).
    /// </summary>
    internal IDisposable BeginSuspend()
    {
        _suspendDepth++;
        return new SuspendScope(this);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        if (_suspendDepth > 0)
        {
            _queuedEvents.Add(name ?? string.Empty);
            return;
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private sealed class SuspendScope : IDisposable
    {
        private readonly SaveWorkSection _owner;
        private bool _disposed;

        public SuspendScope(SaveWorkSection owner) => _owner = owner;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (--_owner._suspendDepth > 0) return;

            var pending = _owner._queuedEvents.ToArray();
            _owner._queuedEvents.Clear();
            foreach (var name in pending)
            {
                _owner.PropertyChanged?.Invoke(
                    _owner,
                    new PropertyChangedEventArgs(name == string.Empty ? null : name));
            }
        }
    }
}
