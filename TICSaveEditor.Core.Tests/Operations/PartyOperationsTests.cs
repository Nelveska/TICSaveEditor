using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Operations;

public class PartyOperationsTests
{
    private static SaveWork NewSaveWorkWithUnits(int populatedCount)
    {
        var sw = new SaveWork(new byte[SaveWork.Size]);
        for (int i = 0; i < populatedCount; i++)
        {
            sw.Battle.Units[i].Character = (byte)(i + 1); // make non-empty
            sw.Battle.Units[i].Job = 1;                   // JobData ID 1 = Squire = ability_flag index 0
        }
        return sw;
    }

    [Fact]
    public void SetAllToLevel_returns_validation_error_when_level_out_of_range()
    {
        var sw = NewSaveWorkWithUnits(3);
        var result = PartyOperations.SetAllToLevel(sw, level: 0);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, i => i.Severity == OperationSeverity.Error);
    }

    [Fact]
    public void SetAllToLevel_sets_level_on_populated_units_skips_empty()
    {
        var sw = NewSaveWorkWithUnits(3);
        var result = PartyOperations.SetAllToLevel(sw, 50);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.UnitsAffected);
        for (int i = 0; i < 3; i++)
            Assert.Equal((byte)50, sw.Battle.Units[i].Level);
        // Units beyond 3 are still empty.
        Assert.True(sw.Battle.Units[3].IsEmpty);
    }

    [Fact]
    public void SetAllToLevel_warns_for_each_empty_slot()
    {
        var sw = NewSaveWorkWithUnits(2);
        var result = PartyOperations.SetAllToLevel(sw, 50);

        var warnings = result.Issues.Count(i => i.Severity == OperationSeverity.Warning);
        Assert.Equal(54 - 2, warnings);
    }

    [Fact]
    public void MaxAllJobPoints_calls_unit_method_for_populated_units()
    {
        var sw = NewSaveWorkWithUnits(2);
        var result = PartyOperations.MaxAllJobPoints(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.UnitsAffected);
        // Verify by checking unit 0's job-point at index 0 is now ushort.MaxValue.
        Assert.Equal(ushort.MaxValue, sw.Battle.Units[0].GetJobPoint(0));
    }

    [Fact]
    public void MaxAllJobPoints_no_op_on_empty_party_returns_zero_affected()
    {
        var sw = new SaveWork(new byte[SaveWork.Size]);
        var result = PartyOperations.MaxAllJobPoints(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.UnitsAffected);
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_succeeds_for_populated_units()
    {
        var sw = NewSaveWorkWithUnits(2);
        var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.UnitsAffected);
        // Job byte 1 (= Squire per JobData.xml) maps to ability_flag index 0.
        // Pre-fix this asserted index 1 (Chemist) — the off-by-one was the user-
        // surfaced M11 bug where Ramza-Squire saw Chemist abilities learned.
        Assert.Equal(0xFF, sw.Battle.Units[0].GetAbilityFlagByte(0, 0));
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_skips_out_of_range_jobs_with_warning()
    {
        // The current 169-entry JobList covers Job 0x00..0xA8. Bytes 0xA9..0xFF
        // are out-of-range and should be skipped with a warning, not throw.
        var sw = NewSaveWorkWithUnits(2);
        sw.Battle.Units[0].Job = 1;      // Squire (in-range, slot 0)
        sw.Battle.Units[1].Job = 0xFE;   // out-of-range

        var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.UnitsAffected);
        Assert.Contains(result.Issues, i =>
            i.Severity == OperationSeverity.Warning &&
            i.Description.Contains("monster, placeholder, or unsupported class"));
        // In-range unit (Squire) → slot 0 set; out-of-range untouched.
        Assert.Equal(0xFF, sw.Battle.Units[0].GetAbilityFlagByte(0, 0));
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_handles_generic_range_jobs()
    {
        // TIC stores generic recruits with Job IDs 0x4A..0x5D (Squire..Mime). These
        // should map to ability_flag slots 0..19. Pre-followup-#3 the op skipped
        // them because the formula was Job-1, treating Job >= 22 as out-of-range.
        var sw = NewSaveWorkWithUnits(3);
        sw.Battle.Units[0].Job = 0x4A;  // Generic Squire
        sw.Battle.Units[1].Job = 0x4B;  // Generic Chemist
        sw.Battle.Units[2].Job = 0x5D;  // Generic Mime

        var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.UnitsAffected);
        Assert.Equal(0xFF, sw.Battle.Units[0].GetAbilityFlagByte(0, 0));   // slot 0 = Squire
        Assert.Equal(0xFF, sw.Battle.Units[1].GetAbilityFlagByte(1, 0));   // slot 1 = Chemist
        Assert.Equal(0xFF, sw.Battle.Units[2].GetAbilityFlagByte(19, 0));  // slot 19 = Mime
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_maps_canonical_class_story_variants_to_slot()
    {
        // Story-character variants whose class name matches a canonical generic
        // (per Docs/JobList.md) all map to that generic's slot. Covers Squire
        // variants (Ramza/Delita), Undead-class variants, and other "unknown
        // usage" canonical-named entries.
        (byte job, int slot)[] cases =
        {
            (0x01, 0),  (0x02, 0),  (0x04, 0),  (0x07, 0),  // Squire variants
            (0x35, 1),                                       // Chemist
            (0x36, 5),                                       // White Mage
            (0x37, 6),                                       // Black Mage
            (0x38, 11),                                      // Mystic
            (0x3D, 2),                                       // Knight (Undead Knight)
            (0x3F, 3),                                       // Archer (Undead Archer)
            (0x42, 6),                                       // Black Mage (Undead)
            (0x44, 7),                                       // Time Mage (Undead)
            (0x46, 11),                                      // Mystic (Undead Oracle)
            (0x47, 8),                                       // Summoner (Undead)
        };

        foreach (var (job, slot) in cases)
        {
            var sw = NewSaveWorkWithUnits(1);
            sw.Battle.Units[0].Job = job;

            var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

            Assert.True(result.Succeeded);
            Assert.Equal(1, result.UnitsAffected);
            Assert.Equal(0xFF, sw.Battle.Units[0].GetAbilityFlagByte(slot, 0));
        }
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_maps_story_unique_classes_to_slot_0()
    {
        // Per community attestation (3 independent sources, 2026-04-29): story-unique
        // class names ALL share slot 0 with Squire. The game interprets the bits per
        // the unit's current class. Covers major roster characters whose class names
        // are NOT in the canonical Squire..Mime set.
        byte[] storyUniqueRosterJobs =
        {
            0x1E, 0x34,                   // Agrias (Holy Knight, two variants)
            0x0D,                         // Orlandeau (Sword Saint)
            0x16, 0x22,                   // Mustadio (Machinist, two variants)
            0x1F,                         // Beowulf (Templar)
            0x2A, 0x2F,                   // Meliadoul (Divine Knight, two variants)
            0x32,                         // Cloud (Soldier)
            0x14, 0x2C, 0x30,             // Alma (Cleric, three variants)
            0x19, 0x29,                   // Rapha (Skyseer, two variants)
            0x12, 0x1A,                   // Marach (Netherseer, two variants)
            0x0C,                         // Ovelia (Princess)
            0x0F, 0x48, 0xA8,             // Reis (Dragonkin / Holy Dragon / Dark Dragon)
            0x11, 0x17, 0xA5,             // Gaffgarion (Fell Knight x2 + Deathknight)
        };

        foreach (byte job in storyUniqueRosterJobs)
        {
            var sw = NewSaveWorkWithUnits(1);
            sw.Battle.Units[0].Job = job;

            var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

            Assert.True(result.Succeeded);
            Assert.Equal(1, result.UnitsAffected);
            // All story-unique classes write to slot 0 (the Squire slot).
            Assert.Equal(0xFF, sw.Battle.Units[0].GetAbilityFlagByte(0, 0));
        }
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_maps_dark_and_onion_knight_to_slots_20_and_21()
    {
        // Slots 20-21 are reserved for Dark Knight + Onion Knight. Both are disabled
        // in TIC but the slots persist (engine inherits the layout from a version
        // that shipped them). 0xA1 and 0xA4 are both Onion Knight per JobList.
        (byte job, int slot)[] cases =
        {
            (0xA0, 20),  // Dark Knight
            (0xA1, 21),  // Onion Knight
            (0xA4, 21),  // Onion Knight (variant)
        };

        foreach (var (job, slot) in cases)
        {
            var sw = NewSaveWorkWithUnits(1);
            sw.Battle.Units[0].Job = job;

            var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

            Assert.True(result.Succeeded);
            Assert.Equal(1, result.UnitsAffected);
            Assert.Equal(0xFF, sw.Battle.Units[0].GetAbilityFlagByte(slot, 0));
        }
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_skips_monsters_and_unknowns_with_warning()
    {
        // Monsters (0x5E..0x8D), Na-shi placeholders (0x8E/0x8F/0x92..0x95), and
        // Unknown-class entries (0x39/0x3A/0x3B/0x9B..0x9F) genuinely have no slot;
        // they are skipped with a warning.
        byte[] noSlotJobs =
        {
            0x5E, 0x70, 0x88,  // Chocobo, Ghoul, Dragon (monsters)
            0x39, 0x9B, 0x9F,  // Unknown class entries
            0x8E,              // Na-shi placeholder
        };

        foreach (byte job in noSlotJobs)
        {
            var sw = NewSaveWorkWithUnits(1);
            sw.Battle.Units[0].Job = job;

            var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

            Assert.True(result.Succeeded);
            Assert.Equal(0, result.UnitsAffected);
            Assert.Contains(result.Issues, i =>
                i.Severity == OperationSeverity.Warning &&
                i.Description.Contains("monster, placeholder, or unsupported class"));
        }
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_skips_no_job_sentinel_with_warning()
    {
        // Job byte 0 = JobData ID 0 = blank entry. Treat as no-job: skip + warn.
        var sw = NewSaveWorkWithUnits(2);
        sw.Battle.Units[0].Job = 1;      // Squire
        sw.Battle.Units[1].Job = 0;      // no-job sentinel

        var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.UnitsAffected);
        Assert.Contains(result.Issues, i =>
            i.Severity == OperationSeverity.Warning &&
            i.Description.Contains("monster, placeholder, or unsupported class"));
    }

    [Fact]
    public void Operations_use_progress_when_provided()
    {
        var sw = NewSaveWorkWithUnits(3);
        var captured = new List<OperationProgressUpdate>();
        var progress = new TestProgress(captured);

        PartyOperations.SetAllToLevel(sw, 50, progress);

        // Progress reports once per unit slot (54 total).
        Assert.Equal(54, captured.Count);
        Assert.Equal(54, captured[^1].Total);
        Assert.Equal(54, captured[^1].Current);
    }

    [Fact]
    public void Operations_throw_on_null_save_work()
    {
        Assert.Throws<ArgumentNullException>(() => PartyOperations.SetAllToLevel(null!, 50));
        Assert.Throws<ArgumentNullException>(() => PartyOperations.MaxAllJobPoints(null!));
        Assert.Throws<ArgumentNullException>(() => PartyOperations.LearnAllAbilitiesCurrentJob(null!));
    }

    [Fact]
    public void SetAllToLevel_validation_error_does_not_mutate_state()
    {
        var sw = NewSaveWorkWithUnits(2);
        var originalLevel = sw.Battle.Units[0].Level;
        PartyOperations.SetAllToLevel(sw, level: 100); // > 99 = error
        Assert.Equal(originalLevel, sw.Battle.Units[0].Level);
    }

    private sealed class TestProgress : IOperationProgress
    {
        private readonly List<OperationProgressUpdate> _updates;
        public TestProgress(List<OperationProgressUpdate> u) => _updates = u;
        public void Report(OperationProgressUpdate u) => _updates.Add(u);
    }
}
