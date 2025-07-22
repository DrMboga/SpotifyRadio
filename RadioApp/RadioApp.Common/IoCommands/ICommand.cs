namespace RadioApp.Common.IoCommands;

/// <summary>
/// IO command sent by PICO IO device
/// </summary>
public interface ICommand
{
    CommandType Type { get; }
}