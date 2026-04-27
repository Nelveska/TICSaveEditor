using System.ComponentModel;
using System.Runtime.CompilerServices;
using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Save;

public class SaveWork : INotifyPropertyChanged, ISnapshotable, ISuspendable
{
    public const int Size = SaveWorkLayout.TotalSize;

    private readonly byte[] _trailingUnk;
    private int _suspendDepth;
    private bool _dirtyDuringSuspend;

    public SaveWork(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException(
                $"SaveWork must be exactly {Size} bytes (got {bytes.Length}).",
                nameof(bytes));
        }

        Card = new CardSection(
            bytes.Slice(SaveWorkLayout.CardOffset, SaveWorkLayout.CardSize));
        Info = new InfoSection(
            bytes.Slice(SaveWorkLayout.InfoOffset, SaveWorkLayout.InfoSize));
        World = new WorldSection(
            bytes.Slice(SaveWorkLayout.WorldOffset, SaveWorkLayout.WorldSize));
        Battle = new BattleSection(
            bytes.Slice(SaveWorkLayout.BattleOffset, SaveWorkLayout.BattleSize));
        User = new UserSection(
            bytes.Slice(SaveWorkLayout.UserOffset, SaveWorkLayout.UserSize));
        FftoWorld = new FftoWorldSection(
            bytes.Slice(SaveWorkLayout.FftoWorldOffset, SaveWorkLayout.FftoWorldSize));
        FftoBattle = new FftoBattleSection(
            bytes.Slice(SaveWorkLayout.FftoBattleOffset, SaveWorkLayout.FftoBattleSize));
        FftoAchievement = new FftoAchievementSection(
            bytes.Slice(SaveWorkLayout.FftoAchievementOffset, SaveWorkLayout.FftoAchievementSize));
        FftoConfig = new FftoConfigSection(
            bytes.Slice(SaveWorkLayout.FftoConfigOffset, SaveWorkLayout.FftoConfigSize));
        FftoBraveStory = new FftoBraveStorySection(
            bytes.Slice(SaveWorkLayout.FftoBraveStoryOffset, SaveWorkLayout.FftoBraveStorySize));

        _trailingUnk = bytes
            .Slice(SaveWorkLayout.TrailingUnkOffset, SaveWorkLayout.TrailingUnkSize)
            .ToArray();
    }

    public CardSection Card { get; }
    public InfoSection Info { get; }
    public WorldSection World { get; }
    public BattleSection Battle { get; }
    public UserSection User { get; }
    public FftoWorldSection FftoWorld { get; }
    public FftoBattleSection FftoBattle { get; }
    public FftoAchievementSection FftoAchievement { get; }
    public FftoConfigSection FftoConfig { get; }
    public FftoBraveStorySection FftoBraveStory { get; }

    public byte[] RawBytes
    {
        get
        {
            var output = new byte[Size];
            var span = output.AsSpan();
            Card.WriteTo(span.Slice(SaveWorkLayout.CardOffset, SaveWorkLayout.CardSize));
            Info.WriteTo(span.Slice(SaveWorkLayout.InfoOffset, SaveWorkLayout.InfoSize));
            World.WriteTo(span.Slice(SaveWorkLayout.WorldOffset, SaveWorkLayout.WorldSize));
            Battle.WriteTo(span.Slice(SaveWorkLayout.BattleOffset, SaveWorkLayout.BattleSize));
            User.WriteTo(span.Slice(SaveWorkLayout.UserOffset, SaveWorkLayout.UserSize));
            FftoWorld.WriteTo(span.Slice(SaveWorkLayout.FftoWorldOffset, SaveWorkLayout.FftoWorldSize));
            FftoBattle.WriteTo(span.Slice(SaveWorkLayout.FftoBattleOffset, SaveWorkLayout.FftoBattleSize));
            FftoAchievement.WriteTo(span.Slice(SaveWorkLayout.FftoAchievementOffset, SaveWorkLayout.FftoAchievementSize));
            FftoConfig.WriteTo(span.Slice(SaveWorkLayout.FftoConfigOffset, SaveWorkLayout.FftoConfigSize));
            FftoBraveStory.WriteTo(span.Slice(SaveWorkLayout.FftoBraveStoryOffset, SaveWorkLayout.FftoBraveStorySize));
            _trailingUnk.AsSpan().CopyTo(span.Slice(SaveWorkLayout.TrailingUnkOffset));
            return output;
        }
    }

    // ===== ISnapshotable =====

    public object CreateSnapshot() => RawBytes;

    public void RestoreFromSnapshot(object snapshot)
    {
        if (snapshot is not byte[] bytes)
            throw new ArgumentException(
                $"SaveWork.RestoreFromSnapshot expected byte[]; got {snapshot?.GetType().Name ?? "null"}.",
                nameof(snapshot));
        if (bytes.Length != Size)
            throw new ArgumentException(
                $"SaveWork snapshot must be exactly {Size} bytes (got {bytes.Length}).",
                nameof(snapshot));

        var span = bytes.AsSpan();
        Card.RehydrateFrom(span.Slice(SaveWorkLayout.CardOffset, SaveWorkLayout.CardSize));
        Info.RehydrateFrom(span.Slice(SaveWorkLayout.InfoOffset, SaveWorkLayout.InfoSize));
        World.RehydrateFrom(span.Slice(SaveWorkLayout.WorldOffset, SaveWorkLayout.WorldSize));
        Battle.RehydrateFrom(span.Slice(SaveWorkLayout.BattleOffset, SaveWorkLayout.BattleSize));
        User.RehydrateFrom(span.Slice(SaveWorkLayout.UserOffset, SaveWorkLayout.UserSize));
        FftoWorld.RehydrateFrom(span.Slice(SaveWorkLayout.FftoWorldOffset, SaveWorkLayout.FftoWorldSize));
        FftoBattle.RehydrateFrom(span.Slice(SaveWorkLayout.FftoBattleOffset, SaveWorkLayout.FftoBattleSize));
        FftoAchievement.RehydrateFrom(span.Slice(SaveWorkLayout.FftoAchievementOffset, SaveWorkLayout.FftoAchievementSize));
        FftoConfig.RehydrateFrom(span.Slice(SaveWorkLayout.FftoConfigOffset, SaveWorkLayout.FftoConfigSize));
        FftoBraveStory.RehydrateFrom(span.Slice(SaveWorkLayout.FftoBraveStoryOffset, SaveWorkLayout.FftoBraveStorySize));
        span.Slice(SaveWorkLayout.TrailingUnkOffset, SaveWorkLayout.TrailingUnkSize)
            .CopyTo(_trailingUnk);

        OnPropertyChanged(null);
    }

    // ===== ISuspendable =====

    public IDisposable SuspendNotifications()
    {
        _suspendDepth++;
        var sections = new IDisposable[]
        {
            Card.BeginSuspend(),
            Info.BeginSuspend(),
            World.BeginSuspend(),
            Battle.BeginSuspend(),
            User.BeginSuspend(),
            FftoWorld.BeginSuspend(),
            FftoBattle.BeginSuspend(),
            FftoAchievement.BeginSuspend(),
            FftoConfig.BeginSuspend(),
            FftoBraveStory.BeginSuspend(),
        };
        return new SuspendScope(this, sections);
    }

    // ===== INPC =====

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        if (_suspendDepth > 0)
        {
            _dirtyDuringSuspend = true;
            return;
        }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private sealed class SuspendScope : IDisposable
    {
        private readonly SaveWork _owner;
        private readonly IDisposable[] _sectionScopes;
        private bool _disposed;

        public SuspendScope(SaveWork owner, IDisposable[] sectionScopes)
        {
            _owner = owner;
            _sectionScopes = sectionScopes;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var scope in _sectionScopes) scope.Dispose();

            if (--_owner._suspendDepth > 0) return;

            if (_owner._dirtyDuringSuspend)
            {
                _owner._dirtyDuringSuspend = false;
                _owner.PropertyChanged?.Invoke(_owner, new PropertyChangedEventArgs(null));
            }
        }
    }
}
