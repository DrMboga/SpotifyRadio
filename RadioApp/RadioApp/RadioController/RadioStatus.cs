using RadioApp.Common.Contracts;
using RadioApp.Common.IoCommands;
using RadioApp.Common.PlayerProcessor;

namespace RadioApp.RadioController;

public class RadioStatus
{
    private readonly ILogger<RadioStatus> _logger;

    /// <summary>
    /// Synchronisation context event. It is set each time the status is changed
    /// </summary>
    public TaskCompletionSource<RadioStatusChangeResult> StatusChanged { get; private set; } =
        new TaskCompletionSource<RadioStatusChangeResult>(TaskCreationOptions.RunContinuationsAsynchronously);

    private object _triggerSync = new object();

    /// <summary>
    /// Current mode of the radio.
    /// L button is for Spotify,
    /// M, K, U buttons are for Internet radio
    /// </summary>
    public PlayerType PlayerType { get; private set; } = PlayerType.Idle;

    /// <summary>
    /// Current internet radio range (for buttons M, K, U)
    /// </summary>
    public SabaRadioButtons SabaRadioButton { get; private set; } = SabaRadioButtons.M;

    /// <summary>
    /// Current status of the radio - play or pause
    /// </summary>
    public PlayerMode PlayMode { get; private set; } = PlayerMode.Pause;

    /// <summary>
    /// Frequency on the radio scale
    /// </summary>
    public int CurrentFrequency { get; private set; } = 105;

    public RadioStatus(ILogger<RadioStatus> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles new input command
    /// </summary>
    /// <param name="command"></param>
    public Task HandleIoCommand(ICommand command)
    {
        var stateChanged = false;
        var result = RadioStatusChangeResult.PlayStateChanged;
        switch (command.Type)
        {
            case CommandType.StatusCommand:
                if (command is StatusCommand statusCommand)
                {
                    _logger.LogDebug("StatusCommand {Button}, {IsPause}, {Frequency} handled",
                        statusCommand.ButtonIndex,
                        statusCommand.IsPause, statusCommand.Frequency);
                    var frequencyWasChanged = IfFrequencyChanged(statusCommand.Frequency);
                    var playPauseWasChanged = IfPlayPausePressed(statusCommand.IsPause);
                    var buttonWasChanged = IfRadioButtonChanged(statusCommand.ButtonIndex);
                    if (buttonWasChanged.StateChanged)
                    {
                        stateChanged = true;
                        result = buttonWasChanged.StateChangeResult;
                    }
                    else if (playPauseWasChanged)
                    {
                        stateChanged = true;
                        result = RadioStatusChangeResult.PlayStateChanged;
                    }
                    else if (frequencyWasChanged)
                    {
                        stateChanged = true;
                        result = RadioStatusChangeResult.FrequencyChanged;
                    }
                }

                break;
            case CommandType.ToggleButtonPressed:
                if (command is ToggleButtonPressedCommand toggleButtonPressed)
                {
                    _logger.LogDebug("ToggleButtonPressedCommand {Button} handled", toggleButtonPressed.ButtonIndex);
                    var state = IfRadioButtonChanged(toggleButtonPressed.ButtonIndex);
                    if (state.StateChanged)
                    {
                        stateChanged = true;
                        result = state.StateChangeResult;
                    }
                }

                break;
            case CommandType.PlayPauseButtonPressed:
                if (command is PlayPauseButtonPressedCommand playPauseButtonPressed)
                {
                    _logger.LogDebug("PlayPauseButtonPressedCommand {Button} handled", playPauseButtonPressed.IsPause);
                    stateChanged = IfPlayPausePressed(playPauseButtonPressed.IsPause);
                    if (stateChanged)
                    {
                        result = RadioStatusChangeResult.PlayStateChanged;
                    }
                }

                break;
            case CommandType.FrequencyChanged:
                if (command is FrequencyChangedCommand frequencyChanged)
                {
                    _logger.LogDebug("FrequencyChangedCommand {Button} handled", frequencyChanged.Frequency);
                    stateChanged = IfFrequencyChanged(frequencyChanged.Frequency);
                    if (stateChanged)
                    {
                        result = RadioStatusChangeResult.FrequencyChanged;
                    }
                }

                break;
        }

        if (stateChanged)
        {
            _logger.LogDebug($"Radio State changed to {result}");
            lock (_triggerSync)
            {
                StatusChanged.TrySetResult(result);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets <see cref="StatusChanged"/> property
    /// </summary>
    public void ResetStatusChangedTrigger()
    {
        if (StatusChanged.Task.IsCompleted)
        {
            lock (_triggerSync)
            {
                StatusChanged =
                    new TaskCompletionSource<RadioStatusChangeResult>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    private (bool StateChanged, RadioStatusChangeResult StateChangeResult) IfRadioButtonChanged(int newButtonIndex)
    {
        if (newButtonIndex < 0)
        {
            // -1 if no button is pressed
            PlayerType = PlayerType.Idle;
            SabaRadioButton = SabaRadioButtons.M;
            return (true, RadioStatusChangeResult.PlayerProcessorChanged);
        }

        var newPlayerType =
            (SabaRadioButtons)((short)newButtonIndex) == SabaRadioButtons.L
                ? PlayerType.Spotify
                : PlayerType.InternetRadio; // L for spotify, M, K, U for internet radio
        if (PlayerType != newPlayerType)
        {
            PlayerType = newPlayerType;
            SabaRadioButton = (SabaRadioButtons)((short)newButtonIndex);
            return (true, RadioStatusChangeResult.PlayerProcessorChanged);
        }

        if ((short)SabaRadioButton == (short)newButtonIndex)
            return (false, RadioStatusChangeResult.RadioRegionChanged);

        SabaRadioButton = (SabaRadioButtons)((short)newButtonIndex);
        return (true, RadioStatusChangeResult.RadioRegionChanged);
    }

    private bool IfPlayPausePressed(bool newPlayPauseState)
    {
        if (PlayMode == PlayerMode.Play && newPlayPauseState) return false;
        PlayMode = newPlayPauseState ? PlayerMode.Play : PlayerMode.Pause;
        return true;
    }

    private bool IfFrequencyChanged(int newFrequency)
    {
        if (CurrentFrequency == newFrequency) return false;
        CurrentFrequency = newFrequency;
        return true;
    }
}