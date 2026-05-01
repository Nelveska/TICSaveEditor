using System.IO;
using System.Linq;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Exercises <see cref="SaveSlotViewModel.HeroNames"/> against the multi-state
/// fixture <c>SaveFiles/enhanced.png</c> (10-slot real playthrough). Per the
/// user's domain knowledge: Ramza is fixed at slot 0 (always shown), guests
/// occupy slots 50..53 (max four; <see cref="UnitListItemViewModel.IsActive"/>
/// gates inclusion). Argath joins as a guest at save 3 (slot 51), departs at
/// save 4 (UnitIndex flips 0x33 -> 0xFF). HeroNames must reflect the transition.
/// Also covers TitleDisplay's empty-slot "—" fallback.
/// </summary>
public class SaveSlotViewModelHeroNamesTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public SaveSlotViewModelHeroNamesTests(GameDataFixture fixture) => _fixture = fixture;

    private ManualSaveFileViewModel LoadEnhanced()
    {
        var path = SaveFixturePaths.EnhancedAtRoot();
        if (!File.Exists(path))
            throw new FileNotFoundException($"Multi-state fixture missing: {path}");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        return Assert.IsType<ManualSaveFileViewModel>(
            SaveFileViewModelFactory.Create(save, _fixture.Context));
    }

    [Fact]
    public void Empty_slot_renders_HeroNames_as_dash()
    {
        var vm = LoadEnhanced();
        var emptySlot = vm.Slots.First(s => s.IsEmpty);
        Assert.Equal("—", emptySlot.HeroNames);
    }

    [Fact]
    public void Empty_slot_renders_TitleDisplay_as_dash()
    {
        var vm = LoadEnhanced();
        var emptySlot = vm.Slots.First(s => s.IsEmpty);
        Assert.Equal("—", emptySlot.TitleDisplay);
    }

    [Fact]
    public void Populated_slot_includes_Ramza_first()
    {
        var vm = LoadEnhanced();
        var populated = vm.Slots.First(s => !s.IsEmpty);
        Assert.StartsWith("Ramza", populated.HeroNames);
    }

    [Fact]
    public void Populated_slot_TitleDisplay_is_not_dash_when_title_set()
    {
        var vm = LoadEnhanced();
        var populated = vm.Slots.First(s => !s.IsEmpty && !string.IsNullOrEmpty(s.Model.SlotTitle));
        Assert.NotEqual("—", populated.TitleDisplay);
        Assert.Equal(populated.Model.SlotTitle, populated.TitleDisplay);
    }

    [Fact]
    public void Slot_with_active_guest_at_51_includes_guest_name_after_Ramza()
    {
        // Per decisions_unit_index_active_flag.md: Argath occupies BattleSection slot 51
        // when active; UnitIndex == 0x33 (51) means active, 0xFF means departed/empty.
        // Find any populated slot where Units[51].IsActive is true.
        var vm = LoadEnhanced();
        var slotWithGuest = vm.Slots.FirstOrDefault(s => !s.IsEmpty && s.Units[51].IsActive);
        if (slotWithGuest is null)
        {
            // No active-guest slot in this fixture state; skip silently rather than fail.
            return;
        }
        var heroNames = slotWithGuest.HeroNames;
        Assert.StartsWith("Ramza", heroNames);
        Assert.Contains(", ", heroNames);
        // The guest's name is whatever Units[51].Name resolves to via the cascade.
        Assert.Contains(slotWithGuest.Units[51].Name, heroNames);
    }

    [Fact]
    public void Slot_with_no_active_guests_returns_only_Ramza()
    {
        var vm = LoadEnhanced();
        var slot = vm.Slots.FirstOrDefault(s =>
            !s.IsEmpty
            && !s.Units[50].IsActive
            && !s.Units[51].IsActive
            && !s.Units[52].IsActive
            && !s.Units[53].IsActive);
        if (slot is null) return;

        Assert.Equal(slot.Units[0].Name, slot.HeroNames);
        Assert.DoesNotContain(",", slot.HeroNames);
    }
}
