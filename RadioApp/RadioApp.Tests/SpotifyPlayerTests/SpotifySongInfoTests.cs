using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.PlayerProcessor;
using RadioApp.Common.Spotify;
using Xunit.Abstractions;

namespace RadioApp.Tests.SpotifyPlayerTests;

[Collection("SequentialTests")]
public class SpotifySongInfoTestSetOne : IClassFixture<SpotifyPlayerFixture>
{
    private readonly SpotifyPlayerFixture _spotifyPlayerFixture;

    public SpotifySongInfoTestSetOne(SpotifyPlayerFixture spotifyPlayerFixture, ITestOutputHelper output)
    {
        _spotifyPlayerFixture = spotifyPlayerFixture;
        _spotifyPlayerFixture.ResetFixture(output);

        _spotifyPlayerFixture.SetupPlayMocks();
    }

    [Fact]
    public async Task ShouldNotStartSongInfoPollingOnPause()
    {
        // Setup
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);

        // Act
        await Task.Delay(4100);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetCurrentlyPlayingInfoRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<ShowSongInfoRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<ShowProgressNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

[Collection("SequentialTests")]
public class SpotifySongInfoTestSetTwo : IClassFixture<SpotifyPlayerFixture>
{
    private readonly SpotifyPlayerFixture _spotifyPlayerFixture;

    public SpotifySongInfoTestSetTwo(SpotifyPlayerFixture spotifyPlayerFixture, ITestOutputHelper output)
    {
        _spotifyPlayerFixture = spotifyPlayerFixture;
        _spotifyPlayerFixture.ResetFixture(output);

        _spotifyPlayerFixture.SetupPlayMocks();
    }

    [Fact]
    public async Task ShouldSendTheSongInfoOnlyOnceForSameSong()
    {
        // Setup
        var songs = Mock.SongsMock();
        _spotifyPlayerFixture.MediatorMock.SetupSequence(m =>
                m.Send(It.IsAny<GetCurrentlyPlayingInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(songs[0])
            .ReturnsAsync(songs[1]);

        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Play, 88);

        // Act
        await Task.Delay(2100);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetCurrentlyPlayingInfoRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<ShowSongInfoRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        int expectedPercent1 = 100 * songs[0].Progress / songs[0].Item!.Duration;
        int expectedPercent2 = 100 * songs[1].Progress / songs[1].Item!.Duration;
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowProgressNotification>(n => n.PercentComplete == expectedPercent1), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowProgressNotification>(n => n.PercentComplete == expectedPercent2), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Collection("SequentialTests")]
public class SpotifySongInfoTestSetThree : IClassFixture<SpotifyPlayerFixture>
{
    private readonly SpotifyPlayerFixture _spotifyPlayerFixture;

    public SpotifySongInfoTestSetThree(SpotifyPlayerFixture spotifyPlayerFixture, ITestOutputHelper output)
    {
        _spotifyPlayerFixture = spotifyPlayerFixture;
        _spotifyPlayerFixture.ResetFixture(output);

        _spotifyPlayerFixture.SetupPlayMocks();
    }

    [Fact]
    public async Task ShouldSendTheSongInfoTwiceForDifferentSong()
    {
        // Setup
        var songs = Mock.SongsMock();
        _spotifyPlayerFixture.MediatorMock.SetupSequence(m =>
                m.Send(It.IsAny<GetCurrentlyPlayingInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(songs[0])
            .ReturnsAsync(songs[2]);

        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Play, 88);

        // Act
        await Task.Delay(2100);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetCurrentlyPlayingInfoRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<ShowSongInfoRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        int expectedPercent1 = 100 * songs[0].Progress / songs[0].Item!.Duration;
        int expectedPercent2 = 100 * songs[2].Progress / songs[2].Item!.Duration;
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowProgressNotification>(n => n.PercentComplete == expectedPercent1), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowProgressNotification>(n => n.PercentComplete == expectedPercent2), It.IsAny<CancellationToken>()), Times.Once);
    }
}

static class SpotifySongInfoTestHelper
{
    public static void SetupPlayMocks(this SpotifyPlayerFixture fixture)
    {
        fixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fixture.SpotifySettings);
        fixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AvailableDevicesResponse()
                        { Id = "barDeviceId", Name = fixture.SpotifySettings.DeviceName },
                ]
            );
        fixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new PlaylistItem()
                        { Id = "barPlayerId", Name = fixture.SpotifySettings.PlaylistName },
                ]
            );
        fixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<StartPlaybackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                true
            );
        fixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<SkipSongRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                true
            );
        fixture.MediatorMock.Setup(m => m.Send(It.IsAny<ShowSongInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        ;
    }
}