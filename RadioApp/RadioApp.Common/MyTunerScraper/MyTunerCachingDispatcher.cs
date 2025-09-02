namespace RadioApp.Common.MyTunerScraper;

public class MyTunerCachingDispatcher
{
    /// <summary>
    /// Synchronisation context event. Runs the process when set
    /// </summary>
    public TaskCompletionSource<bool> StartProcessor { get; private set; } =
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    private object _triggerSync = new object();

    public void SignalForStartProcessing()
    {
        lock (_triggerSync)
        {
            StartProcessor.TrySetResult(true);
        }
    }

    /// <summary>
    /// Resets <see cref="StartProcessor"/> property
    /// </summary>
    public void ResetStatusChangedTrigger()
    {
        if (StartProcessor.Task.IsCompleted)
        {
            lock (_triggerSync)
            {
                StartProcessor = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}