using System.ComponentModel;
using System.Runtime.CompilerServices;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Save;

public class SaveSlot : INotifyPropertyChanged
{
    public SaveSlot(int index, SaveWork saveWork)
    {
        Index = index;
        SaveWork = saveWork;
        SaveWork.Card.PropertyChanged += OnCardPropertyChanged;
        SaveWork.Info.PropertyChanged += OnInfoPropertyChanged;
    }

    public int Index { get; }
    public SaveWork SaveWork { get; }

    public bool IsEmpty => SaveWork.Card.Magic == 0;

    public string SlotTitle
    {
        get => SaveWork.Card.Title;
        set => SaveWork.Card.Title = value;
    }

    public DateTime SaveTimestamp => SaveWork.Card.SaveTimestamp;

    public byte[] HeroNameRaw
    {
        get => SaveWork.Info.HeroNameRaw;
        set => SaveWork.Info.HeroNameRaw = value;
    }

    public TimeSpan Playtime
    {
        get => SaveWork.Info.Playtime;
        set => SaveWork.Info.Playtime = value;
    }

    private void OnCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CardSection.Title):
                OnPropertyChanged(nameof(SlotTitle));
                break;
            case nameof(CardSection.SaveTimestamp):
                OnPropertyChanged(nameof(SaveTimestamp));
                break;
        }
    }

    private void OnInfoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(InfoSection.HeroNameRaw):
                OnPropertyChanged(nameof(HeroNameRaw));
                break;
            case nameof(InfoSection.Playtime):
                OnPropertyChanged(nameof(Playtime));
                break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
