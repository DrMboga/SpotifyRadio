using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.Common.MyTunerScraper;
using RadioApp.Common.PlayerProcessor;

namespace RadioApp.PlayerProcessors;

public class InternetRadioPlayerProcessor : IPlayerProcessor
{
    public PlayerType Type => PlayerType.InternetRadio;
    
    private static readonly TimeSpan UpdateSongInterval = TimeSpan.FromSeconds(2);

    private readonly ILogger<InternetRadioPlayerProcessor> _logger;
    private readonly IMediator _mediator;
    private readonly IRadioVlcPlayer _radioVlcPlayer;
    private readonly PlayerProcessorTimerService _updateSongTimer;

    private readonly PlayerProcessorDebounceFrequencyService _debounceService = new();

    private int _currentFrequency = 0;
    private SabaRadioButtons _currentButton = SabaRadioButtons.M;
    private PlayerMode _currentPlayerMode = PlayerMode.Pause;
    private string? _currentSongTitle;

    public InternetRadioPlayerProcessor(ILogger<InternetRadioPlayerProcessor> logger, IMediator mediator, IRadioVlcPlayer radioVlcPlayer)
    {
        _logger = logger;
        _mediator = mediator;
        _radioVlcPlayer = radioVlcPlayer;
        _updateSongTimer = new PlayerProcessorTimerService(UpdateSongInterval, UpdateSongInfoIfNeeded);
    }

    public async Task Start(SabaRadioButtons currentButton, PlayerMode currentPlayerMode, int currentFrequency)
    {
        _logger.LogInformation("Starting Internet radio player processor");
        _currentFrequency = currentFrequency;
        _currentButton = currentButton;
        _currentPlayerMode = currentPlayerMode;
        await Reset();
    }

    public async Task Stop()
    {
        await _updateSongTimer.Stop();
        _radioVlcPlayer.Stop();
    }

    public async Task Play()
    {
        var currentStation = await _mediator.Send(new GetRadioStationToPlayRequest(_currentButton, _currentFrequency));
        if (currentStation == null || currentStation.StreamUrl == null)
        {
            return;
        }

        _radioVlcPlayer.Play(currentStation.StreamUrl);
        await _updateSongTimer.Start();
        var screenInfo = new RadioScreenInfo
        {
            StationCountry = currentStation.Country ?? string.Empty,
            StationName = currentStation.Name,
            StationCountryFlagBase64 = currentStation.CountryFlagBase64 ?? string.Empty,
            StationLogoBase64 = currentStation.RadioLogoBase64 ?? string.Empty,
        };
        await _mediator.Publish(new ShowRadioStationNotification(screenInfo));
    }

    public Task Pause()
    {
        return Stop();
    }

    public async Task ToggleButtonChanged(SabaRadioButtons button)
    {
        if (_currentButton == button)
        {
            return;
        }

        _currentButton = button;
        await Reset();
    }

    public async Task FrequencyChanged(int frequency)
    {
        if (!_debounceService.CheckLastTime() || frequency == _currentFrequency)
        {
            return;
        }

        _currentFrequency = frequency;
        await Reset();
    }

    private async Task Reset()
    {
        await Stop();
        await _mediator.Publish(new ClearScreenNotification());
        string message = $"{_currentButton} {_currentFrequency} MHz";
        await _mediator.Publish(new ShowFrequencyInfoNotification(message));
        if (_currentPlayerMode == PlayerMode.Play)
        {
            await Play();
        }
    }

    /// <summary>
    /// This method runs every interval to get song info and show it on TFT
    /// </summary>
    private async Task UpdateSongInfoIfNeeded(CancellationToken cancellationToken)
    {
        var title = _radioVlcPlayer.GetCurrentlyPlaying();

        if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(_currentSongTitle))
        {
            await _mediator.Publish(new CleanRadioSongInfoNotification(), cancellationToken);
        }

        if (!string.IsNullOrEmpty(title) && _currentSongTitle != title)
        {
            _currentSongTitle = title;
            await _mediator.Publish(new CleanRadioSongInfoNotification(), cancellationToken);
            await _mediator.Publish(new ShowRadioSongInfoNotification(_currentSongTitle), cancellationToken);
        }
    }
}