namespace RadioApp.Common.IoCommands;

public class FrequencyChangedCommand : ICommand
{
    public CommandType Type => CommandType.FrequencyChanged;

    public int Frequency { get; set; }
}