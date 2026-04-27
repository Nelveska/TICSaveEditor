using System.ComponentModel;
using System.Runtime.CompilerServices;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Records.Entries;

public class JobNewEntry : INotifyPropertyChanged, IRaisableEntry
{
    private readonly FftoBattleSection _owner;

    internal JobNewEntry(FftoBattleSection owner, int index)
    {
        _owner = owner;
        Index = index;
    }

    public int Index { get; }

    public byte Value
    {
        get => _owner.GetJobNewFlag(Index);
        set => _owner.SetJobNewFlag(Index, value);
    }

    void IRaisableEntry.RaiseValueChanged() => OnPropertyChanged(nameof(Value));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
