using MediatR;
using RadioApp.Common.IoCommands;
using RadioApp.Common.Messages.Hardware;

namespace RadioApp.RadioController;

public class CommandsProcessor : INotificationHandler<ProcessCommandNotification>
{
    private readonly ILogger<CommandsProcessor> _logger;

    public CommandsProcessor(ILogger<CommandsProcessor> logger)
    {
        _logger = logger;
    }

    public Task Handle(ProcessCommandNotification notification, CancellationToken cancellationToken)
    {
        switch (notification.Command.Type)
        {
            case CommandType.StatusCommand:
                var statusCommand = notification.Command as StatusCommand;
                _logger.LogDebug("StatusCommand {Button}, {IsPause}, {Frequency} handled", statusCommand?.ButtonIndex,
                    statusCommand?.IsPause, statusCommand?.Frequency);
                break;
            case CommandType.ToggleButtonPressed:
                var toggleButtonPressed = notification.Command as ToggleButtonPressedCommand;
                _logger.LogDebug("ToggleButtonPressedCommand {Button} handled", toggleButtonPressed?.ButtonIndex);
                break;
            case CommandType.PlayPauseButtonPressed:
                var playPauseButtonPressed = notification.Command as PlayPauseButtonPressedCommand;
                _logger.LogDebug("PlayPauseButtonPressedCommand {Button} handled", playPauseButtonPressed?.IsPause);
                break;
            case CommandType.FrequencyChanged:
                var frequencyChanged = notification.Command as FrequencyChangedCommand;
                _logger.LogDebug("FrequencyChangedCommand {Button} handled", frequencyChanged?.Frequency);
                break;
        }

        return Task.CompletedTask;
    }
}