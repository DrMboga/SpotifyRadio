using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.PlayerProcessor;
using RadioApp.Common.Spotify;
using Xunit.Abstractions;

namespace RadioApp.Tests.SpotifyPlayerTests;

public class SpotifyPlaybackTests : IClassFixture<SpotifyPlayerFixture>
{
    private readonly SpotifyPlayerFixture _spotifyPlayerFixture;

    public SpotifyPlaybackTests(SpotifyPlayerFixture spotifyPlayerFixture, ITestOutputHelper output)
    {
        _spotifyPlayerFixture = spotifyPlayerFixture;
        _spotifyPlayerFixture.ResetFixture(output);
    }

    [Fact]
    public async Task ShouldShowApiErrorIfDeviceIdNotFound()
    {
        // Setup
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);

        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AvailableDevicesResponse() { Id = "fooId", Name = "fooName" },
                    new AvailableDevicesResponse() { Id = "barId", Name = "barName" },
                ]
            );

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Play();

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<StartPlaybackRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<ToggleShuffleNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldShowApiErrorIfPlaylistIdNotFound()
    {
        // Setup
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);

        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AvailableDevicesResponse() { Id = "fooId", Name = "fooName" },
                    new AvailableDevicesResponse()
                        { Id = "barId", Name = _spotifyPlayerFixture.SpotifySettings.DeviceName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new PlaylistItem() { Id = "fooId", Name = "fooName" },
                    new PlaylistItem() { Id = "barId", Name = "barName" },
                ]
            );

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Play();

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<StartPlaybackRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<ToggleShuffleNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldShowApiErrorIfPlaybackApiReturnsFalseState()
    {
        // Setup
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AvailableDevicesResponse() { Id = "fooId", Name = "fooName" },
                    new AvailableDevicesResponse()
                        { Id = "barId", Name = _spotifyPlayerFixture.SpotifySettings.DeviceName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new PlaylistItem() { Id = "fooId", Name = "fooName" },
                    new PlaylistItem() { Id = "barId", Name = _spotifyPlayerFixture.SpotifySettings.PlaylistName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<StartPlaybackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                false
            );


        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Play();

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<StartPlaybackRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<ToggleShuffleNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task ShouldStartPlayback()
    {
        // Setup
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AvailableDevicesResponse() { Id = "fooId", Name = "fooName" },
                    new AvailableDevicesResponse()
                        { Id = "barDeviceId", Name = _spotifyPlayerFixture.SpotifySettings.DeviceName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new PlaylistItem() { Id = "fooId", Name = "fooName" },
                    new PlaylistItem()
                        { Id = "barPlayerId", Name = _spotifyPlayerFixture.SpotifySettings.PlaylistName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<StartPlaybackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                true
            );

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Play();

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(
                It.Is<StartPlaybackRequest>(r =>
                    r.DeviceId == "barDeviceId" && r.PlaylistId == "barPlayerId" && r.Resume == false),
                It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<ToggleShuffleNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShouldStartPlaybackThenPauseAndThenResume()
    {
        // Setup
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new AvailableDevicesResponse() { Id = "fooId", Name = "fooName" },
                    new AvailableDevicesResponse()
                        { Id = "barDeviceId", Name = _spotifyPlayerFixture.SpotifySettings.DeviceName },
                ]
            );
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                [
                    new PlaylistItem() { Id = "fooId", Name = "fooName" },
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
            .Setup(m => m.Send(It.IsAny<PausePlaybackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                true
            );

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Play();

        // Pause
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Pause();

        // Resume
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Play();

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetAvailableDevicesRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetSpotifyPlaylistsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(
                It.Is<StartPlaybackRequest>(r =>
                    r.DeviceId == "barDeviceId" && r.PlaylistId == "barPlayerId" && r.Resume == false),
                It.IsAny<CancellationToken>()), Times.Once);

        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(
                It.Is<PausePlaybackRequest>(r =>
                    r.DeviceId == "barDeviceId"),
                It.IsAny<CancellationToken>()), Times.Once);

        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(
                It.Is<StartPlaybackRequest>(r =>
                    r.DeviceId == "barDeviceId" && r.PlaylistId == "barPlayerId" && r.Resume == true),
                It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<ToggleShuffleNotification>(), It.IsAny<CancellationToken>()), Times.Once);

        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
    }
}