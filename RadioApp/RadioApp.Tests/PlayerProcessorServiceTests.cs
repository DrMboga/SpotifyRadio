using Microsoft.Extensions.Logging;
using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.IoCommands;
using RadioApp.Common.PlayerProcessor;
using RadioApp.RadioController;
using Xunit.Abstractions;

namespace RadioApp.Tests;

public class PlayerProcessorServiceTests
{
    private readonly Mock<ILogger<PlayerProcessorService>> _playerProcessorLoggerMock =
        new Mock<ILogger<PlayerProcessorService>>();

    private readonly Mock<ILogger<RadioStatus>> _radioStatusLoggerMock = new Mock<ILogger<RadioStatus>>();

    private readonly Mock<IPlayerProcessor> _idlePlayerProcessorMock = new Mock<IPlayerProcessor>();
    private readonly Mock<IPlayerProcessor> _spotifyPlayerProcessorMock = new Mock<IPlayerProcessor>();

    private readonly Mock<IPlayerProcessor> _internetRadioPlayerProcessorMock =
        new Mock<IPlayerProcessor>();

    private readonly PlayerProcessorFactory _getPlayerProcessor;

    private readonly RadioStatus _radioStatus;

    public PlayerProcessorServiceTests(ITestOutputHelper output)
    {
        _playerProcessorLoggerMock.RegisterTestOutputHelper(output);
        _radioStatusLoggerMock.RegisterTestOutputHelper(output);

        _radioStatus = new RadioStatus(_radioStatusLoggerMock.Object);

        _getPlayerProcessor = playerType =>
        {
            return playerType switch
            {
                PlayerType.Idle => _idlePlayerProcessorMock.Object,
                PlayerType.Spotify => _spotifyPlayerProcessorMock.Object,
                PlayerType.InternetRadio => _internetRadioPlayerProcessorMock.Object,
                _ => throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null)
            };
        };
    }

    [Fact]
    public async Task ShouldSetIdleProcessorByDefault()
    {
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);

        _idlePlayerProcessorMock.Verify(
            p => p.Start(_radioStatus.SabaRadioButton, _radioStatus.PlayMode, _radioStatus.CurrentFrequency),
            Times.Once);
        _spotifyPlayerProcessorMock.Verify(
            p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Never);
        _internetRadioPlayerProcessorMock.Verify(
            p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Never);

        _idlePlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ShouldChangePlayerProcessorToSpotifyOnStatusCommand()
    {
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);

        await Task.Delay(10);

        // Act
        await _radioStatus.HandleIoCommand(new StatusCommand { ButtonIndex = 1, IsPause = false, Frequency = 88 });
        await Task.Delay(10);

        // Assert
        _idlePlayerProcessorMock.Verify(
            p => p.Start(SabaRadioButtons.M, PlayerMode.Pause, 105),
            Times.Once);
        _idlePlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Stop(), Times.Once);

        _spotifyPlayerProcessorMock.Verify(
            p => p.Start(_radioStatus.SabaRadioButton, _radioStatus.PlayMode, _radioStatus.CurrentFrequency),
            Times.Once);
        _internetRadioPlayerProcessorMock.Verify(
            p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ShouldChangePlayerProcessorToInternetRadioOnStatusCommand()
    {
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);

        await Task.Delay(10);

        // Act
        await _radioStatus.HandleIoCommand(new StatusCommand { ButtonIndex = 2, IsPause = false, Frequency = 88 });
        await Task.Delay(10);

        // Assert
        _idlePlayerProcessorMock.Verify(
            p => p.Start(SabaRadioButtons.M, PlayerMode.Pause, 105),
            Times.Once);
        _idlePlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        _idlePlayerProcessorMock.Verify(p => p.Stop(), Times.Once);

        _internetRadioPlayerProcessorMock.Verify(
            p => p.Start(_radioStatus.SabaRadioButton, _radioStatus.PlayMode, _radioStatus.CurrentFrequency),
            Times.Once);
        _spotifyPlayerProcessorMock.Verify(
            p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(2)] // M button (Radio region 1)
    [InlineData(3)] // K button (Radio region 2)
    [InlineData(4)] // U button (Radio region 3)
    public async Task ShouldChangePlayerProcessorFromSpotifyToInternetRadioOnToggleButtonCommand(
        int newRadioButtonIndex)
    {
        // Initially set up the Spotify player mode
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        // Button 1 is L button which is responsible for Spotify
        await _radioStatus.HandleIoCommand(new StatusCommand { ButtonIndex = 1, IsPause = false, Frequency = 88 });
        await Task.Delay(10);

        // Act
        // Button 2 is M button which is responsible for internet radio region 1
        await _radioStatus.HandleIoCommand(new ToggleButtonPressedCommand() { ButtonIndex = newRadioButtonIndex });
        await Task.Delay(10);

        // Assert
        _spotifyPlayerProcessorMock.Verify(
            p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Once);
        _spotifyPlayerProcessorMock.Verify(p => p.Stop(), Times.Once);
        _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);

        _internetRadioPlayerProcessorMock.Verify(
            p => p.Start(_radioStatus.SabaRadioButton, _radioStatus.PlayMode, _radioStatus.CurrentFrequency),
            Times.Once);
        _internetRadioPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(2)] // M button (Radio region 1)
    [InlineData(3)] // K button (Radio region 2)
    [InlineData(4)] // U button (Radio region 3)
    public async Task ShouldChangePlayerProcessorFromInternetRadioToSpotifyOnToggleButtonCommand(
        int initialRadioButtonIndex)
    {
        // Initially set up the Radio player mode
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await _radioStatus.HandleIoCommand(new StatusCommand
            { ButtonIndex = initialRadioButtonIndex, IsPause = false, Frequency = 88 });
        await Task.Delay(10);

        // Act
        await _radioStatus.HandleIoCommand(new ToggleButtonPressedCommand() { ButtonIndex = 1 });
        await Task.Delay(10);

        // Assert
        _internetRadioPlayerProcessorMock.Verify(
            p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Once);
        _internetRadioPlayerProcessorMock.Verify(p => p.Stop(), Times.Once);
        _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);

        _spotifyPlayerProcessorMock.Verify(
            p => p.Start(_radioStatus.SabaRadioButton, _radioStatus.PlayMode, _radioStatus.CurrentFrequency),
            Times.Once);
        _spotifyPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(3)] // K button (Radio region 2)
    [InlineData(4)] // U button (Radio region 3)
    public async Task ShouldChangeRadioRegion(int regionButton)
    {
        // Initially set up the Radio player mode
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await _radioStatus.HandleIoCommand(new StatusCommand { ButtonIndex = 2, IsPause = false, Frequency = 88 });
        await Task.Delay(10);

        // Act
        await _radioStatus.HandleIoCommand(new ToggleButtonPressedCommand() { ButtonIndex = regionButton });
        await Task.Delay(10);

        _internetRadioPlayerProcessorMock.Verify(
            p => p.Start(SabaRadioButtons.M, It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Once);
        _internetRadioPlayerProcessorMock.Verify(
            p => p.Start((SabaRadioButtons)(short)regionButton, It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged((SabaRadioButtons)(short)regionButton),
            Times.Once);
        _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);

        _spotifyPlayerProcessorMock.Verify(
            p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
            Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
        _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(1)] // L button (Spotify)
    [InlineData(2)] // M button (Radio region 1)
    [InlineData(3)] // K button (Radio region 2)
    [InlineData(4)] // U button (Radio region 3)
    public async Task ShouldPlayAndPauseOnCommandPlayOrPause(int initialButtonIndex)
    {
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await _radioStatus.HandleIoCommand(new StatusCommand
            { ButtonIndex = initialButtonIndex, IsPause = true, Frequency = 88 });
        await Task.Delay(10);

        // Act
        // Setup Play mode
        await _radioStatus.HandleIoCommand(new PlayPauseButtonPressedCommand() { IsPause = false });
        await Task.Delay(10);

        // Assert
        if (initialButtonIndex == 1)
        {
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(SabaRadioButtons.L, PlayerMode.Pause, It.IsAny<int>()),
                Times.Once);
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Once);
            _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        }
        else
        {
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start((SabaRadioButtons)initialButtonIndex, PlayerMode.Pause, It.IsAny<int>()),
                Times.Once);
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Once);
            _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        }

        // Set up Pause mode
        await _radioStatus.HandleIoCommand(new PlayPauseButtonPressedCommand() { IsPause = true });
        await Task.Delay(10);

        if (initialButtonIndex == 1)
        {
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(SabaRadioButtons.L, PlayerMode.Pause, It.IsAny<int>()),
                Times.Once);
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Once);
            _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Once);
            _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        }
        else
        {
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start((SabaRadioButtons)initialButtonIndex, PlayerMode.Pause, It.IsAny<int>()),
                Times.Once);
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Once);
            _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Once);
            _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        }
    }

    [Theory]
    [InlineData(1, 90)] // L button (Spotify)
    [InlineData(1, 91)] // L button (Spotify)
    [InlineData(1, 95)] // L button (Spotify)
    [InlineData(1, 104)] // L button (Spotify)
    [InlineData(2, 85)] // M button (Radio region 1)
    [InlineData(2, 86)] // M button (Radio region 1)
    [InlineData(2, 87)] // M button (Radio region 1)
    [InlineData(2, 102)] // M button (Radio region 1)
    [InlineData(2, 103)] // M button (Radio region 1)
    [InlineData(3, 12)] // K button (Radio region 2)
    [InlineData(3, 27)] // K button (Radio region 2)
    [InlineData(3, 48)] // K button (Radio region 2)
    [InlineData(4, 105)] // U button (Radio region 3)
    [InlineData(4, 104)] // U button (Radio region 3)
    [InlineData(4, 103)] // U button (Radio region 3)
    public async Task ShouldChangeFrequency(int initialButtonIndex, int newFrequency)
    {
        // Initially set up the Radio player mode
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await _radioStatus.HandleIoCommand(new StatusCommand
            { ButtonIndex = initialButtonIndex, IsPause = true, Frequency = 0 });
        await Task.Delay(10);

        // Act
        // Setup Play mode
        await _radioStatus.HandleIoCommand(new FrequencyChangedCommand() { Frequency = newFrequency });
        await Task.Delay(10);

        // Assert
        if (initialButtonIndex == 1)
        {
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(SabaRadioButtons.L, PlayerMode.Pause, 0),
                Times.Once);
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(newFrequency), Times.Once);
        }
        else
        {
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start((SabaRadioButtons)initialButtonIndex, PlayerMode.Pause, 0),
                Times.Once);
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Stop(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(newFrequency), Times.Once);
        }
    }

    [Theory]
    [InlineData(1, -1)] // L button (Spotify)
    [InlineData(1, 0)] // L button (Spotify)
    [InlineData(2, -1)] // M button (Radio region 1)
    [InlineData(2, 0)] // M button (Radio region 1)
    [InlineData(3, -1)] // K button (Radio region 2)
    [InlineData(3, 0)] // K button (Radio region 2)
    [InlineData(4, -1)] // U button (Radio region 3)
    [InlineData(4, 0)] // U button (Radio region 3)
    public async Task ShouldSetIdleProcessOnMinusOneOrZeroButtonSelect(int initialButtonIndex, int buttonIndex)
    {
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await _radioStatus.HandleIoCommand(new StatusCommand
            { ButtonIndex = initialButtonIndex, IsPause = true, Frequency = 88 });
        await Task.Delay(10);

        // Act
        await _radioStatus.HandleIoCommand(new ToggleButtonPressedCommand() { ButtonIndex = buttonIndex });
        await Task.Delay(10);
        
        // Assert
        if (initialButtonIndex == 1)
        {
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(SabaRadioButtons.L, PlayerMode.Pause, It.IsAny<int>()),
                Times.Once);
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Stop(), Times.Once);
            _spotifyPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
            _spotifyPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        }
        else
        {
            _internetRadioPlayerProcessorMock.Verify(
                p => p.Start((SabaRadioButtons)initialButtonIndex, PlayerMode.Pause, It.IsAny<int>()),
                Times.Once);
            _spotifyPlayerProcessorMock.Verify(
                p => p.Start(It.IsAny<SabaRadioButtons>(), It.IsAny<PlayerMode>(), It.IsAny<int>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Stop(), Times.Once);
            _internetRadioPlayerProcessorMock.Verify(p => p.ToggleButtonChanged(It.IsAny<SabaRadioButtons>()),
                Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Play(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.Pause(), Times.Never);
            _internetRadioPlayerProcessorMock.Verify(p => p.FrequencyChanged(It.IsAny<int>()), Times.Never);
        }
        
        _idlePlayerProcessorMock.Verify(
            p => p.Start(SabaRadioButtons.M, PlayerMode.Pause, It.IsAny<int>()),
            Times.Exactly(2));
    }
}