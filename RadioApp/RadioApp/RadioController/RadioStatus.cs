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
    public TaskCompletionSource StatusChanged { get; } =
        new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
        switch (command.Type)
        {
            case CommandType.StatusCommand:
                if (command is StatusCommand statusCommand)
                {
                    _logger.LogDebug("StatusCommand {Button}, {IsPause}, {Frequency} handled",
                        statusCommand.ButtonIndex,
                        statusCommand.IsPause, statusCommand.Frequency);
                    stateChanged = IfRadioButtonChanged(statusCommand.ButtonIndex) |
                                   IfPlayPausePressed(statusCommand.IsPause) |
                                   IfFrequencyChanged(statusCommand.Frequency);
                }

                break;
            case CommandType.ToggleButtonPressed:
                if (command is ToggleButtonPressedCommand toggleButtonPressed)
                {
                    _logger.LogDebug("ToggleButtonPressedCommand {Button} handled", toggleButtonPressed.ButtonIndex);
                    stateChanged = IfRadioButtonChanged(toggleButtonPressed.ButtonIndex);
                }

                break;
            case CommandType.PlayPauseButtonPressed:
                if (command is PlayPauseButtonPressedCommand playPauseButtonPressed)
                {
                    _logger.LogDebug("PlayPauseButtonPressedCommand {Button} handled", playPauseButtonPressed.IsPause);
                    stateChanged = IfPlayPausePressed(playPauseButtonPressed.IsPause);
                }

                break;
            case CommandType.FrequencyChanged:
                if (command is FrequencyChangedCommand frequencyChanged)
                {
                    _logger.LogDebug("FrequencyChangedCommand {Button} handled", frequencyChanged.Frequency);
                    stateChanged = IfFrequencyChanged(frequencyChanged.Frequency);
                }

                break;
        }

        if (stateChanged)
        {
            StatusChanged.TrySetResult();
        }

        return Task.CompletedTask;
    }

    private bool IfRadioButtonChanged(int newButtonIndex)
    {
        if ((short)SabaRadioButton == (short)newButtonIndex) return false;
        if (newButtonIndex < 0)
        {
            // -1 if no button is pressed
            PlayerType = PlayerType.Idle;
            SabaRadioButton = SabaRadioButtons.M;
            return true;
        }

        SabaRadioButton = (SabaRadioButtons)((short)newButtonIndex);
        PlayerType =
            SabaRadioButton == SabaRadioButtons.L
                ? PlayerType.Spotify
                : PlayerType.InternetRadio; // L for spotify, M, K, U for internet radio
        return true;
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