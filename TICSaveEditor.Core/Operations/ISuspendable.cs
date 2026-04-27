namespace TICSaveEditor.Core.Operations;

public interface ISuspendable
{
    IDisposable SuspendNotifications();
}
