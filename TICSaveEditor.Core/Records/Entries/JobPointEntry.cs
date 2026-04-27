using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records.Entries;

public class JobPointEntry : INotifyPropertyChanged, IRaisableEntry
{
    private readonly UnitSaveData _owner;

    internal JobPointEntry(UnitSaveData owner, int jobId)
    {
        _owner = owner;
        JobId = jobId;
    }

    public int JobId { get; }

    public ushort Value
    {
        get => _owner.GetJobPoint(JobId);
        set => _owner.SetJobPoint(JobId, value);
    }

    void IRaisableEntry.RaiseValueChanged() => OnPropertyChanged(nameof(Value));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
