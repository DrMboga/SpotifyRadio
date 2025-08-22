using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RadioApp.PlayerProcessors;
using Xunit.Abstractions;

namespace RadioApp.Tests.SpotifyPlayerTests;

public class SpotifyPlayerFixture
{
    private readonly Mock<ILogger<SpotifyPlayerProcessor>> _spotifyPlayerProcessorLoggerMock = new();

    public Mock<IMediator> MediatorMock { get; } = new();
    public SpotifyPlayerProcessor SpotifyPlayerProcessor { get; private set; }

    public Common.Contracts.SpotifySettings SpotifySettings { get; private set; }

    public void ResetFixture(ITestOutputHelper output)
    {
        _spotifyPlayerProcessorLoggerMock.RegisterTestOutputHelper(output);
        SpotifySettings = new Common.Contracts.SpotifySettings
        {
            ClientId = "Fake client id",
            ClientSecret = "Fake client secret",
            RedirectUrl = "http://fake.url",
            AuthToken = "Fake auth token",
            AuthTokenExpiration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000,
            RefreshToken = "Fake refresh token",
            DeviceName = "Fake device name",
            PlaylistName = "Fake playlist name",
        };
        MediatorMock.Invocations.Clear();
        SpotifyPlayerProcessor = new SpotifyPlayerProcessor(_spotifyPlayerProcessorLoggerMock.Object, MediatorMock.Object);
    }

}