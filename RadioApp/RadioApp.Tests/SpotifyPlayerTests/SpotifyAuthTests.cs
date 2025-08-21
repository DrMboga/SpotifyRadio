using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.PlayerProcessor;
using RadioApp.Common.Spotify;
using Xunit.Abstractions;

namespace RadioApp.Tests.SpotifyPlayerTests;

public class SpotifyAuthTests : IClassFixture<SpotifyPlayerFixture>
{
    private readonly SpotifyPlayerFixture _spotifyPlayerFixture;

    public SpotifyAuthTests(SpotifyPlayerFixture spotifyPlayerFixture, ITestOutputHelper output)
    {
        _spotifyPlayerFixture = spotifyPlayerFixture;
        _spotifyPlayerFixture.ResetFixture(output);
    }

    [Fact]
    public async Task ShouldShowAuthErrorScreenOnEmptySettings()
    {
        // Setup
        var settings = _spotifyPlayerFixture.SpotifySettings;
        settings.AuthToken = null;
        settings.AuthTokenExpiration = null;
        settings.RefreshToken = null;

        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShouldNotShowAuthErrorAndNotCheckAuthTokenIfInitializedWithPause()
    {
        // Setup
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldNotRefreshTokenIfTokenExpiredButInitializedWithPause()
    {
        var settings = _spotifyPlayerFixture.SpotifySettings;
        settings.AuthTokenExpiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600000;
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Pause, 88);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<RefreshSpotifyAuthTokenRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldRefreshTokenIfTokenExpiredButInitializedWithPlay()
    {
        // Setup
        var settings = _spotifyPlayerFixture.SpotifySettings;
        settings.AuthTokenExpiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600000;
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var refreshTokenResponse = new RefreshTokenResponse()
        {
            AccessToken = "New AccessToken",
            RefreshToken = "New RefreshToken",
            ExpiresIn = 3600,
        };
        _spotifyPlayerFixture.MediatorMock.Setup(m =>
                m.Send(It.IsAny<RefreshSpotifyAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenResponse);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Play, 88);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<RefreshSpotifyAuthTokenRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);

        var expectedExpiration = refreshTokenResponse.ExpiresIn * 1000 +
                                 DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        _spotifyPlayerFixture.MediatorMock.Verify(m => m.Publish(It.Is<SetSpotifySettingsNotification>(n =>
            n.SpotifySettings.AuthToken == refreshTokenResponse.AccessToken
            && n.SpotifySettings.RefreshToken == refreshTokenResponse.RefreshToken
            && n.SpotifySettings.AuthTokenExpiration <= expectedExpiration + 1000
            && n.SpotifySettings.AuthTokenExpiration >= expectedExpiration - 1000
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShouldNotRefreshTokenIfTokenExpiredButInitializedWithPlay()
    {
        // Setup
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_spotifyPlayerFixture.SpotifySettings);
        
        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Play, 88);
        
        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<RefreshSpotifyAuthTokenRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldShowApiErrorScreenOnAuthTokenRefreshError()
    {
                // Setup
        var settings = _spotifyPlayerFixture.SpotifySettings;
        settings.AuthTokenExpiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600000;
        _spotifyPlayerFixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetSpotifySettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var refreshTokenResponse = new RefreshTokenResponse();
        _spotifyPlayerFixture.MediatorMock.Setup(m =>
                m.Send(It.IsAny<RefreshSpotifyAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenResponse);

        // Act
        await _spotifyPlayerFixture.SpotifyPlayerProcessor.Start(SabaRadioButtons.L, PlayerMode.Play, 88);

        // Assert
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyAuthError.bmp"),
                It.IsAny<CancellationToken>()), Times.Never);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<RefreshSpotifyAuthTokenRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _spotifyPlayerFixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowStaticImageNotification>(n => n.AssetName == "SpotifyApiError.bmp"),
                It.IsAny<CancellationToken>()), Times.Once);

        var expectedExpiration = refreshTokenResponse.ExpiresIn * 1000 +
                                 DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        _spotifyPlayerFixture.MediatorMock.Verify(m => m.Publish(It.IsAny<SetSpotifySettingsNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}