using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.Common.PlayerProcessor;
using Xunit.Abstractions;

namespace RadioApp.Tests.InternetRadioPlayerTests;

public class InternetRadioPlayerTest : IClassFixture<InternetRadioFixture>
{
    private readonly InternetRadioFixture _fixture;

    public InternetRadioPlayerTest(InternetRadioFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.ResetFixture(output);
    }


    [Fact]
    public async Task ShouldSetUpScreenOnInit()
    {
        int frequency = 88;
        // Act
        await _fixture.InternetRadioPlayerProcessor.Start(SabaRadioButtons.M, PlayerMode.Pause, frequency);

        // Assert
        _fixture.RadioVlcPlayerMock.Verify(p => p.Stop(), Times.Once);
        _fixture.MediatorMock.Verify(m => m.Publish(It.IsAny<ClearScreenNotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        string message = $"M {frequency} MHz";
        _fixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowFrequencyInfoNotification>(n => n.FrequencyInfo == message),
                It.IsAny<CancellationToken>()), Times.Once);

        _fixture.MediatorMock.Verify(
            m => m.Send(It.IsAny<GetRadioStationToPlayRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldPlayIfDbContainsStationInfo()
    {
        // Setup
        string songTitle = "FakeSong";
        _fixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetRadioStationToPlayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.MockRadioStation);
        _fixture.RadioVlcPlayerMock.SetupSequence(p => p.GetCurrentlyPlaying())
            .Returns(songTitle)
            .Returns(string.Empty);

        // Act
        await _fixture.InternetRadioPlayerProcessor.Start(_fixture.MockRadioStation.Button, PlayerMode.Play,
            _fixture.MockRadioStation.SabaFrequency);

        // Assert
        _fixture.MediatorMock.Verify(
            m => m.Send(
                It.Is<GetRadioStationToPlayRequest>(r =>
                    r.Button == _fixture.MockRadioStation.Button &&
                    r.Frequency == _fixture.MockRadioStation.SabaFrequency), It.IsAny<CancellationToken>()),
            Times.Once);
        _fixture.RadioVlcPlayerMock.Verify(p => p.Play(_fixture.MockRadioStation.StreamUrl!), Times.Once);
        _fixture.MediatorMock.Verify(m =>
                m.Publish(It.Is<ShowRadioStationNotification>(n =>
                    n.ScreenInfo.StationCountry == _fixture.MockRadioStation.Country &&
                    n.ScreenInfo.StationCountryFlagBase64 == _fixture.MockRadioStation.CountryFlagBase64 &&
                    n.ScreenInfo.StationLogoBase64 == _fixture.MockRadioStation.RadioLogoBase64 &&
                    n.ScreenInfo.StationName == _fixture.MockRadioStation.Name
                ), It.IsAny<CancellationToken>()),
            Times.Once);

        _fixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<CleanRadioSongInfoNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _fixture.MediatorMock.Verify(
            m => m.Publish(It.Is<ShowRadioSongInfoNotification>(n => n.SongInfo == songTitle),
                It.IsAny<CancellationToken>()), Times.Once);

        await Task.Delay(2100);

        // Should clean up the song info on empty string
        _fixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<CleanRadioSongInfoNotification>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _fixture.MediatorMock.Verify(
            m => m.Publish(It.IsAny<ShowRadioSongInfoNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShouldNotPlayIfNoStationInDb()
    {
        _fixture.MediatorMock
            .Setup(m => m.Send(It.IsAny<GetRadioStationToPlayRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RadioStation?)null);

        // Act
        await _fixture.InternetRadioPlayerProcessor.Start(_fixture.MockRadioStation.Button, PlayerMode.Play,
            _fixture.MockRadioStation.SabaFrequency);

        // Assert
        _fixture.MediatorMock.Verify(
            m => m.Send(
                It.Is<GetRadioStationToPlayRequest>(r =>
                    r.Button == _fixture.MockRadioStation.Button &&
                    r.Frequency == _fixture.MockRadioStation.SabaFrequency), It.IsAny<CancellationToken>()),
            Times.Once);
        _fixture.RadioVlcPlayerMock.Verify(p => p.Play(It.IsAny<string>()), Times.Never);
        _fixture.MediatorMock.Verify(m =>
                m.Publish(It.IsAny<ShowRadioStationNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}