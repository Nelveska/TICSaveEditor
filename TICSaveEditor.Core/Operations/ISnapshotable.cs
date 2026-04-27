namespace TICSaveEditor.Core.Operations;

public interface ISnapshotable
{
    object CreateSnapshot();
    void RestoreFromSnapshot(object snapshot);
}
