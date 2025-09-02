using MediatR;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.Common.MyTunerScraper;

namespace RadioApp.MyTunerBackgroundScraper;

public class MyTunerCachingBackgroundService : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<MyTunerCachingBackgroundService> _logger;
    private readonly MyTunerCachingDispatcher _myTunerCachingDispatcher;
    private readonly IMediator _mediator;
    private readonly CancellationTokenSource _cancellationTokenRef = new();
    private Task? _listenToSignalTask;

    public MyTunerCachingBackgroundService(MyTunerCachingDispatcher myTunerCachingDispatcher,
        ILogger<MyTunerCachingBackgroundService> logger, IMediator mediator)
    {
        _myTunerCachingDispatcher = myTunerCachingDispatcher;
        _logger = logger;
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--== Starting MyTuner Background caching service ==--");
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _cancellationTokenRef.Token);
        _listenToSignalTask = Task.Run(() => ListenToSignal(linkedCts.Token), linkedCts.Token);
        await CheckForUncachedStations(stoppingToken);
    }

    private async Task ListenToSignal(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _myTunerCachingDispatcher.StartProcessor.Task.WaitAsync(cancellationToken);
                _logger.LogDebug("Got a signal to run background MyTuner stations caching");

                var stationsToParse = await _mediator.Send(new GetRadioStationsForCachingRequest(), cancellationToken);
                _logger.LogDebug($"Got {stationsToParse.Length} radio stations to cache");
                foreach (var radioStationInfo in stationsToParse)
                {
                    // polite delay + jitter
                    await Task.Delay(Random.Shared.Next(400, 900), cancellationToken);
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    var parsedStation = await _mediator.Send(new ParseRadioStationRequest(radioStationInfo), cancellationToken);
                    parsedStation.StationProcessed = true;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await _mediator.Publish(new UpdateRadioStationInfoNotification(parsedStation), cancellationToken);
                }

                _myTunerCachingDispatcher.ResetStatusChangedTrigger();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "My Tuner Background Caching service Exception");
        }
    }

    private async Task CheckForUncachedStations(CancellationToken cancellationToken)
    {
        var stationsToParse = await _mediator.Send(new GetRadioStationsForCachingRequest(), cancellationToken);
        if (stationsToParse.Length > 0)
        {
            _myTunerCachingDispatcher.SignalForStartProcessing();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenRef.CancelAsync();
        if (_listenToSignalTask is not null)
        {
            try
            {
                await _listenToSignalTask;
            }
            catch
            {
                /* Swallow on dispose */
            }
        }

        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }
}