using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records.Entries;

public class JobLevelEntry : INotifyPropertyChanged, IRaisableEntry
{
    private readonly UnitSaveData _owner;

    internal JobLevelEntry(UnitSaveData owner, int jobId)
    {
        _owner = owner;
        JobId = jobId;
    }

    public int JobId { get; }

    public byte Value
    {
        get => _owner.GetJobLevel(JobId);
        set => _owner.SetJobLevel(JobId, value);
    }

    void IRaisableEntry.RaiseValueChanged() => OnPropertyChanged(nameof(Value));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
