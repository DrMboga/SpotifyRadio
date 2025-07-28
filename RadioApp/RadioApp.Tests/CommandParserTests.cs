using RadioApp.Common.IoCommands;
using RadioApp.Hardware.Helpers;

namespace RadioApp.Tests;

public class CommandParserTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    public void ShouldParseToggleButtonCommandText(int buttonIndex)
    {
        var commandText = $"{{\"command\":\"ButtonPressed\",\"buttonIndex\":{buttonIndex}}}";

        var command = commandText.ParseCommand();

        Assert.NotNull(command);
        Assert.Equal(CommandType.ToggleButtonPressed, command.Type);
        Assert.Equal(buttonIndex, (command as ToggleButtonPressedCommand)!.ButtonIndex);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldParsePlayPauseButtonCommandText(bool playPauseButton)
    {
        var commandText = $"{{\"command\":\"PlayPause\",\"isPause\":{(playPauseButton ? 1: 0)}}}";

        var command = commandText.ParseCommand();

        Assert.NotNull(command);
        Assert.Equal(CommandType.PlayPauseButtonPressed, command.Type);
        Assert.Equal(playPauseButton, (command as PlayPauseButtonPressedCommand)!.IsPause);
    }

    [Theory]
    [InlineData(84)]
    [InlineData(98)]
    [InlineData(103)]
    public void ShouldParseFrequencyChangedCommandText(int frequency)
    {
        var commandText = $"{{\"command\":\"NewFrequency\",\"frequency\":{frequency}}}";

        var command = commandText.ParseCommand();

        Assert.NotNull(command);
        Assert.Equal(CommandType.FrequencyChanged, command.Type);
        Assert.Equal(frequency, (command as FrequencyChangedCommand)!.Frequency); 
    }

    [Fact]
    public void ShouldParseStateCommandText()
    {
        var commandText = $"{{\"command\":\"State\",\"buttonIndex\":2,\"isPause\":0,\"frequency\":101}}";
        
        var command = commandText.ParseCommand();

        Assert.NotNull(command);
        Assert.Equal(CommandType.StatusCommand, command.Type);
        Assert.Equal(101, (command as StatusCommand)!.Frequency); 
        Assert.Equal(2, (command as StatusCommand)!.ButtonIndex); 
        Assert.False((command as StatusCommand)!.IsPause); 
    }
}