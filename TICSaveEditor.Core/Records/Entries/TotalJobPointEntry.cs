using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records.Entries;

public class TotalJobPointEntry : INotifyPropertyChanged, IRaisableEntry
{
    private readonly UnitSaveData _owner;

    internal TotalJobPointEntry(UnitSaveData owner, int jobId)
    {
        _owner = owner;
        JobId = jobId;
    }

    public int JobId { get; }

    public ushort Value
    {
        get => _owner.GetTotalJobPoint(JobId);
        set => _owner.SetTotalJobPoint(JobId, value);
    }

    void IRaisableEntry.RaiseValueChanged() => OnPropertyChanged(nameof(Value));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
