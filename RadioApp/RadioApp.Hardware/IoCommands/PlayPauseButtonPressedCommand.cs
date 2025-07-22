namespace RadioApp.Hardware.IoCommands;

public class PlayPauseButtonPressedCommand : ICommand
{
    public CommandType Type => CommandType.PlayPauseButtonPressed;

    public bool IsPause { get; set; }
}