using System.Text.Json.Nodes;
using RadioApp.Common.IoCommands;

namespace RadioApp.Hardware.Helpers;

public static class CommandsParserHelper
{
    public static ICommand ParseCommand(this string command)
    {
        var doc = JsonNode.Parse(command);
        var commandType = (doc?["command"]) ?? throw new ArgumentException("Unknown command type");
        var commandTypeName = commandType.GetValue<string>();
        return commandTypeName switch
        {
            "ButtonPressed" => ParseButtonPressedNode(doc),
            "PlayPause" => ParsePlayPauseCommand(doc),
            "NewFrequency" => ParseFrequencyChangedCommand(doc),
            "State" => ParseStatusCommand(doc),
            _ => throw new ArgumentException($"Unknown command type '{commandTypeName}'"),
        };
    }
    
    private static ToggleButtonPressedCommand ParseButtonPressedNode(JsonNode commandNode)
    {
        var buttonIndex = (commandNode["buttonIndex"]?.GetValue<int>()) ?? throw new ArgumentException("Unknown button index");
        return new ToggleButtonPressedCommand {ButtonIndex = buttonIndex};
    }
    private static PlayPauseButtonPressedCommand ParsePlayPauseCommand(JsonNode commandNode)
    {
        var isPause = (commandNode["isPause"]?.GetValue<int>()) ?? throw new ArgumentException("Unknown play/pause status");
        return new PlayPauseButtonPressedCommand {IsPause = isPause != 0};
    }
    private static FrequencyChangedCommand ParseFrequencyChangedCommand(JsonNode commandNode)
    {
        var frequency = (commandNode["frequency"]?.GetValue<int>()) ?? throw new ArgumentException("Unknown frequency");
        return new FrequencyChangedCommand {Frequency = frequency};
    }
    private static StatusCommand ParseStatusCommand(JsonNode commandNode)
    {
        var buttonIndex = (commandNode["buttonIndex"]?.GetValue<int>()) ?? throw new ArgumentException("Unknown button index");
        var isPause = (commandNode["isPause"]?.GetValue<int>()) ?? throw new ArgumentException("Unknown play/pause status");
        var frequency = (commandNode["frequency"]?.GetValue<int>()) ?? throw new ArgumentException("Unknown frequency");
        return new StatusCommand
        {
            ButtonIndex = buttonIndex,
            IsPause = isPause != 0,
            Frequency = frequency
        };
    }
}