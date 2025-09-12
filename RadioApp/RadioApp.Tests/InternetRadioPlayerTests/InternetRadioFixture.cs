using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RadioApp.Common.Contracts;
using RadioApp.Common.MyTunerScraper;
using RadioApp.PlayerProcessors;
using Xunit.Abstractions;

namespace RadioApp.Tests.InternetRadioPlayerTests;

public class InternetRadioFixture
{
    private readonly Mock<ILogger<InternetRadioPlayerProcessor>> _radioPlayerProcessorMock = new();
    public Mock<IMediator> MediatorMock { get; } = new();
    public Mock<IRadioVlcPlayer> RadioVlcPlayerMock { get; } = new();

    public RadioStation MockRadioStation { get; } = new()
    {
        Button = SabaRadioButtons.M,
        Country = "FakeCountry",
        Name = "FakeName",
        SabaFrequency = 89,
        StationDetailsUrl = "https://www.fake-station-details.com",
        StreamUrl = "https://www.fake-stream-url.com",
        CountryFlagBase64 = "data:image/png;base64,...",
        RadioLogoBase64 = "data:image/jpg;base64,..."
    };
    
    public InternetRadioPlayerProcessor InternetRadioPlayerProcessor { get; private set; }

    public void ResetFixture(ITestOutputHelper output)
    {
        _radioPlayerProcessorMock.RegisterTestOutputHelper(output);
        MediatorMock.Invocations.Clear();
        RadioVlcPlayerMock.Invocations.Clear();
        InternetRadioPlayerProcessor = new InternetRadioPlayerProcessor(_radioPlayerProcessorMock.Object,
            MediatorMock.Object, RadioVlcPlayerMock.Object);
    }
}