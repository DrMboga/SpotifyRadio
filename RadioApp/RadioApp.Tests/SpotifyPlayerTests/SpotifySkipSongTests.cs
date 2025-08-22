using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.PlayerProcessor;
using RadioApp.Common.Spotify;
using Xunit.Abstractions;

namespace RadioApp.Tests.SpotifyPlayerTests;

public class SpotifySkipSongTests : IClassFixture<SpotifyPlayerFixture>
{
    private readonly SpotifyPlayerFixture _spotifyPlayerFixture;

    public SpotifySkipSongTests(SpotifyPlayerFixture spotifyPlayerFixture, ITestOutputHelper output)
    {
        _spotifyPlayerFixture = spotifyPlayerFixture;
        _spotifyPlayerFixture.ResetFixture(output);

        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AvailableDevicesResponse()
                        { Id = "barDeviceId", Name = _spotifyPlayerFixture.SpotifySettings.DeviceName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new PlaylistItem()
                        { Id = "barPlayerId", Name = _spotifyPlayerFixture.SpotifySettings.PlaylistName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<StartPlaybackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                true
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<SkipSongRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                true
            );
    }

    [Fact]
    public async Task ShouldDebounceSkipSong()
    {
        // Setup
        var initialFrequency = 88;
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Play, initialFrequency);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.FrequencyChanged(initialFrequency + 1);
        // Change frequency back immediately
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.FrequencyChanged(initialFrequency);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<SkipSongRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldNotSkipSongIfOnPause()
    {
        // Setup
        var initialFrequency = 88;
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause,
            initialFrequency);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.FrequencyChanged(initialFrequency + 1);
        await Task.Delay(510);
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.FrequencyChanged(initialFrequency);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<SkipSongRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldSkipSong(bool next)
    {
        // Setup
        var initialFrequency = 88;
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Play, initialFrequency);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.FrequencyChanged(next
            ? initialFrequency + 1
            : initialFrequency - 1);
        await Task.Delay(510);
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.FrequencyChanged(initialFrequency);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.Is<SkipSongRequest>(r => r.SkipToNext == next), It.IsAny<CancellationToken>()), Times.Once);
    }
}