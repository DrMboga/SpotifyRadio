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
    public SpotifyPlayerProcessor SpotifyPlayerProcessor { get; }

    public SpotifyPlayerFixture()
    {
        SpotifyPlayerProcessor = new SpotifyPlayerProcessor(_spotifyPlayerProcessorLoggerMock.Object, MediatorMock.Object);
    }

    public void SetOutput(ITestOutputHelper output)
    {
        _spotifyPlayerProcessorLoggerMock.RegisterTestOutputHelper(output);
    }

}