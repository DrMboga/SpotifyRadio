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
    private readonly Mock<ILogger<PlayerProcessorService>> _playerProcessorLoggerMock = new Mock<ILogger<PlayerProcessorService>>();
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

    [Fact]
    public async Task ShouldChangePlayerProcessorFromSpotifyToInternetRadioOnToggleButtonCommand()
    {
        // Initially set up the Spotify player mode
        var playerProcessorService = new PlayerProcessorService(
            _playerProcessorLoggerMock.Object, _radioStatus, _getPlayerProcessor);

        await playerProcessorService.StartAsync(CancellationToken.None);
        await Task.Delay(10);
        await _radioStatus.HandleIoCommand(new StatusCommand { ButtonIndex = 1, IsPause = false, Frequency = 88 });
        await Task.Delay(10);

        // Act
        await _radioStatus.HandleIoCommand(new ToggleButtonPressedCommand() { ButtonIndex = 2 });
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
}