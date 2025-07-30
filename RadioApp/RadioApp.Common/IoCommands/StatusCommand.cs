namespace RadioApp.Common.IoCommands;

public class StatusCommand: ICommand
{
    public CommandType Type => CommandType.StatusCommand;
    
    /// <summary>
    /// 0: Phono | 1: L | 2: M | 3: K | 4: U
    /// </summary>
    public int ButtonIndex { get; set; }
    public bool IsPause { get; set; }
    public int Frequency { get; set; }
}