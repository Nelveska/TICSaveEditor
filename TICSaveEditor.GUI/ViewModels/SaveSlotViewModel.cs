using System;
using System.Collections.ObjectModel;
using System.Text;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// Wraps a single <see cref="SaveSlot"/> for display. ResumeWorld synthesises a
/// <see cref="SaveSlot"/> with <see cref="SaveSlot.Index"/> = -1 to reuse this VM.
/// All bindings are one-way for M10.
/// </summary>
public class SaveSlotViewModel : ViewModelBase
{
    public SaveSlotViewModel(SaveSlot model, GameDataContext gameData)
    {
        Model = model;
        var unitVms = new UnitListItemViewModel[BattleSection.UnitCount];
        for (int i = 0; i < BattleSection.UnitCount; i++)
        {
            unitVms[i] = new UnitListItemViewModel(model.SaveWork.Battle.Units[i], i, gameData);
        }
        Units = new ReadOnlyObservableCollection<UnitListItemViewModel>(
            new ObservableCollection<UnitListItemViewModel>(unitVms));
    }

    public SaveSlot Model { get; }

    public int Index => Model.Index;
    public bool IsEmpty => Model.IsEmpty;
    public bool IsNotEmpty => !Model.IsEmpty;
    public string SlotLabel => Index < 0 ? "Resume" : $"Slot {Index}";
    public string Title => Model.SlotTitle;

    public string SaveTimestampDisplay
    {
        get
        {
            // Card.SaveTimestamp returns DateTimeKind.Utc per CardSection.cs:52.
            // Slots with Magic != 0 but Unix-epoch-0 timestamps render as "—"
            // (the user reported "1969-12-31 15:59" rows in their live save —
            // those are slots whose IsEmpty heuristic failed; rendering them
            // as "—" is honest about not knowing the real save date).
            var ts = Model.SaveTimestamp;
            if (ts == DateTime.UnixEpoch) return "—";
            var local = DateTime.SpecifyKind(ts, DateTimeKind.Utc).ToLocalTime();
            return local.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public string PlaytimeDisplay
    {
        get
        {
            // Playtime in TIC manual saves at InfoSection offset 0x74 reads 0
            // for valid populated saves with correct timestamps. Real bug —
            // offset is wrong or playtime is stored elsewhere. Render "—" until
            // a SaveDiff fixture pinpoints the correct offset (deferred to v0.2).
            var pt = Model.Playtime;
            if (pt == TimeSpan.Zero) return "—";
            return $"{(int)pt.TotalHours}h {pt.Minutes:D2}m";
        }
    }

    public string HeroName
    {
        get
        {
            // TIC manual saves zero-fill InfoSection.HeroNameRaw — names are
            // resolved via name_no + locale lookup (CLAUDE.md gotcha). Raw ASCII
            // names only appear in battle-format files, which v0.1 doesn't edit.
            // Display "—" so the empty bytes don't masquerade as missing data.
            // Real fix: resolve via NameNo lookup at viewmodel layer (v0.2+).
            var raw = Model.HeroNameRaw;
            if (Array.TrueForAll(raw, b => b == 0)) return "—";
            var nullIdx = Array.IndexOf(raw, (byte)0);
            var len = nullIdx < 0 ? raw.Length : nullIdx;
            return Encoding.ASCII.GetString(raw, 0, len);
        }
    }

    public ReadOnlyObservableCollection<UnitListItemViewModel> Units { get; }
}
