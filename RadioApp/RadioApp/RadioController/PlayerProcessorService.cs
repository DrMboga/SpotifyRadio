using MediatR;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.PlayerProcessor;

namespace RadioApp.RadioController;

/// <summary>
/// This is a background service which waits the event from <see cref="RadioStatus"/> singelton instance.
/// It controls the main media playback process according to the commands from <see cref="RadioStatus"/>
/// </summary>
public class PlayerProcessorService : BackgroundService
{
    private readonly ILogger<PlayerProcessorService> _logger;
    private readonly RadioStatus _radioStatus;
    private readonly PlayerProcessorFactory _getPlayerProcessor;
    private readonly IMediator _mediator;

    private IPlayerProcessor _currentPlayerProcessor;


    public PlayerProcessorService(ILogger<PlayerProcessorService> logger, RadioStatus radioStatus,
        PlayerProcessorFactory getPlayerProcessor, IMediator mediator)
    {
        _logger = logger;
        _radioStatus = radioStatus;
        _getPlayerProcessor = getPlayerProcessor;
        _mediator = mediator;

        _currentPlayerProcessor = _getPlayerProcessor(PlayerType.Idle);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--== Starting Player Processor ==--");
        await _mediator.Publish(new InitDisplayNotification());
        await _currentPlayerProcessor.Start(_radioStatus.SabaRadioButton, _radioStatus.PlayMode,
            _radioStatus.CurrentFrequency);
        while (!stoppingToken.IsCancellationRequested)
        {
            // Waiting the sync event from IO input
            var newState = await _radioStatus.StatusChanged.Task.WaitAsync(stoppingToken);
            _radioStatus.ResetStatusChangedTrigger();

            // New state arrived
            _logger.LogDebug($"New state is {newState}");
            switch (newState)
            {
                case RadioStatusChangeResult.PlayerProcessorChanged:
                    // Change player processor
                    await _currentPlayerProcessor.Stop();
                    _currentPlayerProcessor = _getPlayerProcessor(_radioStatus.PlayerType);
                    await _currentPlayerProcessor.Start(_radioStatus.SabaRadioButton, _radioStatus.PlayMode,
                        _radioStatus.CurrentFrequency);
                    break;
                case RadioStatusChangeResult.RadioRegionChanged:
                    await _currentPlayerProcessor.ToggleButtonChanged(_radioStatus.SabaRadioButton);
                    break;
                case RadioStatusChangeResult.PlayStateChanged:
                    if (_radioStatus.PlayMode == PlayerMode.Play)
                    {
                        await _currentPlayerProcessor.Play();
                    }
                    else
                    {
                        await _currentPlayerProcessor.Pause();
                    }

                    break;
                case RadioStatusChangeResult.FrequencyChanged:
                    await _currentPlayerProcessor.FrequencyChanged(_radioStatus.CurrentFrequency);
                    break;
                default:
                    continue;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--== Stopping Player Processor ==--");
        await _currentPlayerProcessor.Stop();
    }
}