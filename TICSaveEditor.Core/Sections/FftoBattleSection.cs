using System.Collections.ObjectModel;
using TICSaveEditor.Core.Records.Entries;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class FftoBattleSection : SaveWorkSection
{
    public const int JobNewFlagCount = 21;
    public const int JobDisableFlagCount = 125;
    public const int GuideArrivalFlagsLength = 48;

    private const int JobNewOffset = 0x00;
    private const int JobDisableOffset = 0x15;
    private const int GuideArrivalOffset = 0x92;

    private readonly JobNewEntry[] _jobNewEntries;
    private readonly JobDisableEntry[] _jobDisableEntries;

    internal FftoBattleSection(ReadOnlySpan<byte> bytes) : base(bytes)
    {
        _jobNewEntries = new JobNewEntry[JobNewFlagCount];
        for (int i = 0; i < JobNewFlagCount; i++)
            _jobNewEntries[i] = new JobNewEntry(this, i);
        JobNewFlags = new ReadOnlyObservableCollection<JobNewEntry>(
            new ObservableCollection<JobNewEntry>(_jobNewEntries));

        _jobDisableEntries = new JobDisableEntry[JobDisableFlagCount];
        for (int i = 0; i < JobDisableFlagCount; i++)
            _jobDisableEntries[i] = new JobDisableEntry(this, i);
        JobDisableFlags = new ReadOnlyObservableCollection<JobDisableEntry>(
            new ObservableCollection<JobDisableEntry>(_jobDisableEntries));
    }

    internal override int Size => SaveWorkLayout.FftoBattleSize;

    // 21 bytes at offset 0x00 (template field: unit_job.job_new). Most plausible semantic
    // based on the template label is the "NEW!" badge state in the job-change menu —
    // set the first time a character actually changes into a given job (per-character × per-job
    // granularity is the open question, 21 × 8 bits = 168 fits 22 jobs × ~8 chars). Confirm via
    // SaveDiff: change one character to a job they've never been; one byte/bit should flip.
    public ReadOnlyObservableCollection<JobNewEntry> JobNewFlags { get; }

    // 125 bytes at offset 0x15 (template field: unit_job.disable_flag). Semantics OPEN —
    // no in-game UI disables a job manually. Candidate hypotheses to test via SaveDiff:
    //   1. Internal job-eligibility filters (creature-type restrictions, character-locked jobs
    //      like Sword Saint = Orlandu-only, gender-locked Bard/Dancer).
    //   2. The Dark Knight / Onion Knight slots (jobs 20 and 21) being disabled in this game
    //      version — both jobs were stripped from the released game (carried from the mobile
    //      predecessor). Their bytes might be pre-set to non-zero on a vanilla save.
    //   3. Mode-restricted jobs (e.g., NPC-only).
    // 125 bytes is much larger than the 22 generic jobs — could be a 1000-bit array indexed
    // into the 176-entry JobData table, or some other granularity. Pure observation of a fresh
    // vanilla save's initial values will start narrowing.
    public ReadOnlyObservableCollection<JobDisableEntry> JobDisableFlags { get; }

    public byte[] GuideArrivalFlagsRaw
    {
        get
        {
            var copy = new byte[GuideArrivalFlagsLength];
            Bytes.AsSpan(GuideArrivalOffset, GuideArrivalFlagsLength).CopyTo(copy);
            return copy;
        }
    }

    public byte GetJobNewFlag(int index)
    {
        if ((uint)index >= JobNewFlagCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return Bytes[JobNewOffset + index];
    }

    public void SetJobNewFlag(int index, byte value)
    {
        if ((uint)index >= JobNewFlagCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (Bytes[JobNewOffset + index] == value) return;
        Bytes[JobNewOffset + index] = value;
        ((IRaisableEntry)_jobNewEntries[index]).RaiseValueChanged();
        OnPropertyChanged(nameof(JobNewFlags));
    }

    public byte GetJobDisableFlag(int index)
    {
        if ((uint)index >= JobDisableFlagCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        return Bytes[JobDisableOffset + index];
    }

    public void SetJobDisableFlag(int index, byte value)
    {
        if ((uint)index >= JobDisableFlagCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (Bytes[JobDisableOffset + index] == value) return;
        Bytes[JobDisableOffset + index] = value;
        ((IRaisableEntry)_jobDisableEntries[index]).RaiseValueChanged();
        OnPropertyChanged(nameof(JobDisableFlags));
    }
}
