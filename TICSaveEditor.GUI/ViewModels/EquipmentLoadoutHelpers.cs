using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// Shared filter/option-builder helpers for the per-unit loadout editors
/// (CombatSet preset editor and Live inline editor). Owns the per-slot
/// ItemCategory permitted-sets and the synthetic-Unknown / synthetic-Empty
/// ComboBox plumbing that's common to both editors.
///
/// History: extracted from <see cref="CombatSetEntryViewModel"/> private
/// statics 2026-05-01 when the Live editor (which mirrors the same filtering
/// shape against inline UnitData fields) needed access. Behaviour-preserving
/// move; the only NEW helper is <see cref="BuildSecondarySkillsetOptions"/>
/// (single-skillset variant, byte-clamped, for the inline SecondaryAction
/// field). See <c>decisions_inline_equip_5slot_demux.md</c>.
/// </summary>
internal static class EquipmentLoadoutHelpers
{
    public const string ReactionAbilityType = "Reaction";
    public const string SupportAbilityType = "Support";
    public const string MovementAbilityType = "Movement";

    // JobCommand Keys 1–3 are universal battle-menu commands (Attack / Evasive
    // Stance / Reequip), not skillsets. Real skillsets start at Key=5 (Fundaments);
    // Key=4 has null Name (already dropped by name filter). Filter is c.Id > this.
    public const int MaxNonSkillsetJobCommandId = 3;

    // Abilities to exclude wholesale. Keys 0/510: AbilityType=None placeholders
    // (defensive — type filter already drops them). Key 508: "Stealth" — exists
    // and functional, but applied via a different code path; setting it here is
    // non-functional. Key 509: "Treasure Hunter" — crashes the game when used
    // (per user 2026-05-01).
    public static readonly HashSet<int> ExcludedAbilityIds = new() { 0, 508, 509, 510 };

    // Display-name overrides for placeholder-named abilities ("A###") that are
    // actually functional. Key 483 ("A483") is a debug utility known as "CT 0"
    // / "No Charge" — leftover from development, often modded back in.
    public static readonly Dictionary<int, string> AbilityRenames = new()
    {
        { 483, "CT0/No Charge" },
    };

    // Matches developer placeholder names like "A483", "A508" — literal "A" +
    // digits + end-of-string. Real ability names (Aero, Auto-Potion, Aim, etc.)
    // don't match (must end with non-digit characters).
    public static readonly Regex PlaceholderAbilityNameRegex =
        new(@"^A\d+$", RegexOptions.Compiled);

    // ItemCategory permitted sets, per FFT-domain knowledge user-attested
    // 2026-05-01 (see decisions_combatset_item_accessors.md). RH and LH share —
    // shields can be in either hand. "Bag" is a weapon class in FFT (food bags),
    // NOT an accessory. "Cloak" is an accessory (cape-type item), NOT body
    // armor. "Throwing" is shuriken consumables, used via Throw, not equipped.
    // Excluded everywhere (consumables / non-equipment): None, Item, Bomb, Throwing.
    public static readonly IReadOnlySet<string> RhLhCategories = new HashSet<string>
    {
        "Knife", "NinjaBlade", "Sword", "Axe", "Flail", "Polearm", "Pole", "Rod",
        "Staff", "Bow", "Crossbow", "Gun", "Instrument", "Book", "Katana",
        "KnightSword", "Bag", "Shield",
    };
    public static readonly IReadOnlySet<string> HeadCategories = new HashSet<string>
    {
        "Helmet", "Hat", "HairAdornment",
    };
    public static readonly IReadOnlySet<string> ArmorCategories = new HashSet<string>
    {
        "Armor", "Robe", "Cloth", "Clothing",
    };
    public static readonly IReadOnlySet<string> AccessoryCategories = new HashSet<string>
    {
        "Armguard", "Armlet", "Cloak", "Perfume", "Ring", "Shoes",
    };

    public static IReadOnlyList<JobInfo> BuildJobOptions(byte currentJob, GameDataContext gameData)
    {
        var filtered = gameData.Jobs.Where(j => !string.IsNullOrEmpty(j.Name)).ToList();
        if (filtered.Any(j => j.Id == currentJob)) return filtered;
        var augmented = new List<JobInfo>(filtered.Count + 1)
        {
            new JobInfo(currentJob, $"Unknown Job (ID {currentJob})", string.Empty,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        };
        augmented.AddRange(filtered);
        return augmented;
    }

    public static IReadOnlyList<JobCommandInfo> BuildSkillsetOptions(short s0, short s1, GameDataContext gameData)
    {
        var filtered = gameData.JobCommands
            .Where(c => !string.IsNullOrEmpty(c.Name) && c.Id > MaxNonSkillsetJobCommandId)
            .ToList();
        var needS0 = !filtered.Any(c => c.Id == s0);
        var needS1 = !filtered.Any(c => c.Id == s1);
        if (!needS0 && !needS1) return filtered;
        var augmented = new List<JobCommandInfo>(filtered.Count + 2);
        if (needS0) augmented.Add(new JobCommandInfo(s0, $"Unknown Skillset (ID {s0})", string.Empty));
        if (needS1 && s1 != s0) augmented.Add(new JobCommandInfo(s1, $"Unknown Skillset (ID {s1})", string.Empty));
        augmented.AddRange(filtered);
        return augmented;
    }

    /// <summary>
    /// Single-skillset variant for the inline SecondaryAction field (u8 storage
    /// at unit+0x07). Adds <c>c.Id &lt;= byte.MaxValue</c> to the dual-skillset
    /// filter so values that can't round-trip through the byte storage don't
    /// appear in the picker.
    /// </summary>
    public static IReadOnlyList<JobCommandInfo> BuildSecondarySkillsetOptions(byte current, GameDataContext gameData)
    {
        var filtered = gameData.JobCommands
            .Where(c => !string.IsNullOrEmpty(c.Name)
                && c.Id > MaxNonSkillsetJobCommandId
                && c.Id <= byte.MaxValue)
            .ToList();
        if (filtered.Any(c => c.Id == current)) return filtered;
        var augmented = new List<JobCommandInfo>(filtered.Count + 1)
        {
            new JobCommandInfo(current, $"Unknown Skillset (ID {current})", string.Empty),
        };
        augmented.AddRange(filtered);
        return augmented;
    }

    public static IReadOnlyList<AbilityInfo> BuildAbilityOptionsByType(
        ushort current, GameDataContext gameData, string typeFilter)
    {
        var filtered = gameData.Abilities
            .Where(a =>
                !ExcludedAbilityIds.Contains(a.Id)
                && !string.IsNullOrEmpty(a.Name)
                && a.AbilityType == typeFilter
                && !a.Name.StartsWith("MARKED FOR DELETION", StringComparison.Ordinal)
                && (!PlaceholderAbilityNameRegex.IsMatch(a.Name) || AbilityRenames.ContainsKey(a.Id)))
            .Select(a => AbilityRenames.TryGetValue(a.Id, out var renamed)
                ? a with { Name = renamed }
                : a)
            .ToList();
        if (filtered.Any(a => a.Id == current)) return filtered;
        var augmented = new List<AbilityInfo>(filtered.Count + 1)
        {
            new AbilityInfo(current, $"Unknown Ability (ID {current})", string.Empty, 0, 0, string.Empty),
        };
        augmented.AddRange(filtered);
        return augmented;
    }

    /// <summary>
    /// Builds the option list for one equipment slot. Always prepends a
    /// synthetic "(Empty)" entry at index 0 carrying the 0x00FF empty-equip
    /// sentinel — a legitimate persisted value, distinct from the
    /// "filter-miss Unknown" sentinel below. When the current persisted ID is
    /// neither 0x00FF nor in the filtered set (mod ID, wrong-category, retired
    /// item), an additional synthetic "Unknown Item (ID n)" entry is inserted
    /// after Empty so SelectedItem can resolve to it (preserves byte-faithful
    /// round-trip).
    /// </summary>
    public static IReadOnlyList<ItemInfo> BuildItemOptionsBySlot(
        ushort current, GameDataContext gameData, IReadOnlySet<string> permittedCategories)
    {
        var filtered = gameData.Items
            .Where(i =>
                !string.IsNullOrEmpty(i.Name)
                && permittedCategories.Contains(i.ItemCategory))
            .ToList();

        var emptySynthetic = new ItemInfo(UnitSaveData.EmptyEquipSlotSentinel, "(Empty)",
            string.Empty, string.Empty, string.Empty, string.Empty, 0, 0);

        bool needUnknown = current != UnitSaveData.EmptyEquipSlotSentinel
            && !filtered.Any(i => i.Id == current);

        var augmented = new List<ItemInfo>(filtered.Count + 2) { emptySynthetic };
        if (needUnknown)
            augmented.Add(new ItemInfo(current, $"Unknown Item (ID {current})",
                string.Empty, string.Empty, string.Empty, string.Empty, 0, 0));
        augmented.AddRange(filtered);
        return augmented;
    }
}
