namespace RadioApp.Hardware.IoCommands;

public class ToggleButtonPressedCommand : ICommand
{
    public CommandType Type => CommandType.ToggleButtonPressed;

    public int ButtonIndex { get; set; }
}