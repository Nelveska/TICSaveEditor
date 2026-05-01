using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// View model for one of a unit's three <see cref="CombatSet"/> presets. Wraps
/// the typed accessors (Name, Job, Skillset0/1, Reaction/Support/MovementAbility)
/// with ComboBox-friendly Selected* properties backed by per-entry option lists.
///
/// Option lists are filtered:
/// - <see cref="ReactionOptions"/>/<see cref="SupportOptions"/>/<see cref="MovementOptions"/>
///   filter <c>gameData.Abilities</c> by <c>AbilityType</c> exact-match ("Reaction"/
///   "Support"/"Movement") and exclude: null/empty names, IDs in
///   <see cref="ExcludedAbilityIds"/> (crash-prone or non-functional in this slot),
///   "MARKED FOR DELETION ..." cut-content names, and "A###" placeholder names
///   unless the ID has an entry in <see cref="AbilityRenames"/> (rare functional
///   abilities with developer-placeholder names — e.g. Key 483 = "CT0/No Charge").
/// - <see cref="JobOptions"/> excludes null/empty names.
/// - <see cref="SkillsetOptions"/> excludes null/empty names AND Keys 1–3 (basic
///   battle-menu commands Attack/Evasive Stance/Reequip — not skillsets).
/// - In all cases, when the *current* persisted ID isn't in the *filtered* list
///   (mod ID, wrong-type ability, retired entry), a synthetic "Unknown … (ID n)"
///   entry is prepended — preserves byte-faithful round-trip and keeps SelectedItem
///   non-null. See <c>decisions_combatset_editor_ui.md</c>.
///
/// IsDoubleHand intentionally NOT exposed in v0.1: byte at section-relative 0x57
/// is speculative (Nenkai's i16 Job spans 0x56..0x57; no DoubleHand toggle SaveDiff
/// fixture exists), and toggling on a unit lacking the DoubleHand support/innate
/// has unknown safety. Core <c>CombatSet.IsDoubleHand</c> retained for byte-faithful
/// round-trip but no UI consumer.
/// </summary>
public class CombatSetEntryViewModel : ViewModelBase
{
    private const string ReactionAbilityType = "Reaction";
    private const string SupportAbilityType = "Support";
    private const string MovementAbilityType = "Movement";

    // JobCommand Keys 1–3 are universal battle-menu commands (Attack / Evasive
    // Stance / Reequip), not skillsets. Real skillsets start at Key=5 (Fundaments);
    // Key=4 has null Name (already dropped by name filter). Filter is c.Id > this.
    private const int MaxNonSkillsetJobCommandId = 3;

    // Abilities to exclude wholesale. Keys 0/510: AbilityType=None placeholders
    // (defensive — type filter already drops them). Key 508: "Stealth" — exists
    // and functional, but applied via a different code path; setting it here is
    // non-functional. Key 509: "Treasure Hunter" — crashes the game when used
    // (per user 2026-05-01). Without this set, Treasure Hunter would silently
    // ship in the Movement combo with its full vanilla name + description.
    private static readonly HashSet<int> ExcludedAbilityIds = new() { 0, 508, 509, 510 };

    // Display-name overrides for placeholder-named abilities ("A###") that are
    // actually functional. Key 483 ("A483") is a debug utility known as "CT 0"
    // / "No Charge" — leftover from development, often modded back in. Without
    // this rename it would be filtered out by the placeholder regex below.
    private static readonly Dictionary<int, string> AbilityRenames = new()
    {
        { 483, "CT0/No Charge" },
    };

    // Matches developer placeholder names like "A483", "A508" — literal "A" +
    // digits + end-of-string. Real ability names (Aero, Auto-Potion, Aim, etc.)
    // don't match (must end with non-digit characters).
    private static readonly Regex PlaceholderAbilityNameRegex =
        new(@"^A\d+$", RegexOptions.Compiled);

    private readonly CombatSet _model;

    public CombatSetEntryViewModel(CombatSet model, GameDataContext gameData)
    {
        _model = model;

        JobOptions = BuildJobOptions(model.Job, gameData);
        SkillsetOptions = BuildSkillsetOptions(model.Skillset0, model.Skillset1, gameData);
        ReactionOptions = BuildAbilityOptionsByType(model.ReactionAbility, gameData, ReactionAbilityType);
        SupportOptions = BuildAbilityOptionsByType(model.SupportAbility, gameData, SupportAbilityType);
        MovementOptions = BuildAbilityOptionsByType(model.MovementAbility, gameData, MovementAbilityType);

        _model.PropertyChanged += OnModelPropertyChanged;
    }

    public CombatSet Model => _model;
    public int Index => _model.Index;
    public string Header => $"Set {_model.Index}";

    public string Name
    {
        get => _model.Name;
        set => _model.Name = value;
    }

    public IReadOnlyList<JobInfo> JobOptions { get; }
    public JobInfo? SelectedJob
    {
        get => JobOptions.FirstOrDefault(j => j.Id == _model.Job);
        set { if (value is not null) _model.Job = (byte)value.Id; }
    }

    public IReadOnlyList<JobCommandInfo> SkillsetOptions { get; }
    public JobCommandInfo? SelectedSkillset0
    {
        get => SkillsetOptions.FirstOrDefault(c => c.Id == _model.Skillset0);
        set { if (value is not null) _model.Skillset0 = (short)value.Id; }
    }
    public JobCommandInfo? SelectedSkillset1
    {
        get => SkillsetOptions.FirstOrDefault(c => c.Id == _model.Skillset1);
        set { if (value is not null) _model.Skillset1 = (short)value.Id; }
    }

    public IReadOnlyList<AbilityInfo> ReactionOptions { get; }
    public AbilityInfo? SelectedReaction
    {
        get => ReactionOptions.FirstOrDefault(a => a.Id == _model.ReactionAbility);
        set { if (value is not null) _model.ReactionAbility = (ushort)value.Id; }
    }

    public IReadOnlyList<AbilityInfo> SupportOptions { get; }
    public AbilityInfo? SelectedSupport
    {
        get => SupportOptions.FirstOrDefault(a => a.Id == _model.SupportAbility);
        set { if (value is not null) _model.SupportAbility = (ushort)value.Id; }
    }

    public IReadOnlyList<AbilityInfo> MovementOptions { get; }
    public AbilityInfo? SelectedMovement
    {
        get => MovementOptions.FirstOrDefault(a => a.Id == _model.MovementAbility);
        set { if (value is not null) _model.MovementAbility = (ushort)value.Id; }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CombatSet.Name): OnPropertyChanged(nameof(Name)); break;
            case nameof(CombatSet.Job): OnPropertyChanged(nameof(SelectedJob)); break;
            case nameof(CombatSet.Skillset0): OnPropertyChanged(nameof(SelectedSkillset0)); break;
            case nameof(CombatSet.Skillset1): OnPropertyChanged(nameof(SelectedSkillset1)); break;
            case nameof(CombatSet.ReactionAbility): OnPropertyChanged(nameof(SelectedReaction)); break;
            case nameof(CombatSet.SupportAbility): OnPropertyChanged(nameof(SelectedSupport)); break;
            case nameof(CombatSet.MovementAbility): OnPropertyChanged(nameof(SelectedMovement)); break;
            case null:
                // Bulk re-raise on snapshot rehydrate (CLAUDE.md M8 pattern).
                OnPropertyChanged((string?)null);
                break;
        }
    }

    private static IReadOnlyList<JobInfo> BuildJobOptions(byte currentJob, GameDataContext gameData)
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

    private static IReadOnlyList<JobCommandInfo> BuildSkillsetOptions(short s0, short s1, GameDataContext gameData)
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

    private static IReadOnlyList<AbilityInfo> BuildAbilityOptionsByType(
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
}
