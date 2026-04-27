namespace TICSaveEditor.Core.Operations;

public interface IOperationProgress
{
    void Report(OperationProgressUpdate update);
}
