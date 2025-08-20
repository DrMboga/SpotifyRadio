using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.PlayerProcessor;
using Xunit.Abstractions;

namespace RadioApp.Tests.SpotifyPlayerTests;

public class SpotifyAuthTests : IClassFixture<SpotifyPlayerFixture>
{
    private readonly SpotifyPlayerFixture _spotifyPlayerFixture;

    public SpotifyAuthTests(SpotifyPlayerFixture spotifyPlayerFixture, ITestOutputHelper output)
    {
        _spotifyPlayerFixture = spotifyPlayerFixture;
        _spotifyPlayerFixture.SetOutput(output);
    }

    [Fact]
    public async Task ShouldShowAuthErrorScreenOnEmptySettings()
    {
        // Setup
        var settings = new Common.Contracts.SpotifySettings
        {
            ClientId = "Fake client id",
            ClientSecret = "Fake client secret",
            RedirectUrl = "http://fake.url"
        };
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
}