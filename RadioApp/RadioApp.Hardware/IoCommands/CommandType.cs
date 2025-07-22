namespace RadioApp.Hardware.IoCommands;

/// <summary>
/// Types of commands sent by PICO IO
/// </summary>
public enum CommandType
{
    StatusCommand,
    ToggleButtonPressed,
    PlayPauseButtonPressed,
    FrequencyChanged
}