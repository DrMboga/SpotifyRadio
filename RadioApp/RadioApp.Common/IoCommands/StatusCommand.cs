namespace RadioApp.Common.IoCommands;

public class StatusCommand: ICommand
{
    public CommandType Type => CommandType.StatusCommand;
    
    public int ButtonIndex { get; set; }
    public bool IsPause { get; set; }
    public int Frequency { get; set; }
}