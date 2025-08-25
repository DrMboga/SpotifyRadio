namespace RadioApp.PlayerProcessors;

public class PlayerProcessorTimerService : IAsyncDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly Func<CancellationToken, Task> _tick;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cancellationTokenSource;

    private Task? _runTask;

    public PlayerProcessorTimerService(TimeSpan interval, Func<CancellationToken, Task> tick)
    {
        _timeProvider = TimeProvider.System;
        _interval = interval;
        _tick = tick;
    }

    public bool IsRunning { get; private set; }

    public Task Start()
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _runTask = Task.Run(() => RunTimer(_cancellationTokenSource.Token));
        IsRunning = true;
        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _cancellationTokenSource?.Cancel();
            if (_runTask != null)
            {
                try
                {
                    await _runTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    /* ignore */
                }
            }
        }
        finally
        {
            _runTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            IsRunning = false;
        }
    }

    private async Task RunTimer(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval, _timeProvider);
        
        // Do an immediate tick for the first time
        await _tick(cancellationToken).ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await _tick(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Stop();
    }
}