using MediatR;
using Microsoft.Extensions.Logging;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.RadioStreaming.WebScraper;

namespace RadioApp.RadioStreaming;

public class MyTunerScraper :
    IRequestHandler<GetMyTunerCountriesListRequest, MyTunerCountryInfo[]>,
    INotificationHandler<StartCacheMyTunerStationsByCountryNotification>,
    IRequestHandler<ParseRadioStationRequest, RadioStationInfo>
{
    private readonly ILogger<MyTunerScraper> _logger;
    private readonly MyTunerCountriesScrapper _myTunerCountriesScrapper;
    private readonly MyTunerStationsScraper _myTunerStationsScraper;
    private readonly MyTunerStationInfoScraper _myTunerStationInfoScraper;

    public MyTunerScraper(ILogger<MyTunerScraper> logger, MyTunerCountriesScrapper myTunerCountriesScrapper,
        MyTunerStationsScraper myTunerStationsScraper, MyTunerStationInfoScraper myTunerStationInfoScraper)
    {
        _logger = logger;
        _myTunerCountriesScrapper = myTunerCountriesScrapper;
        _myTunerStationsScraper = myTunerStationsScraper;
        _myTunerStationInfoScraper = myTunerStationInfoScraper;
    }

    public Task<MyTunerCountryInfo[]> Handle(GetMyTunerCountriesListRequest request,
        CancellationToken cancellationToken)
    {
        return _myTunerCountriesScrapper.GetCountries(cancellationToken);
    }

    public Task Handle(StartCacheMyTunerStationsByCountryNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            $"Radio station caching for '{notification.CountryInfo.Country}': '{notification.CountryInfo.Url}'");
        return _myTunerStationsScraper.StartCachingStations(notification.CountryInfo.Country,
            notification.CountryInfo.Url);
    }

    public async Task<RadioStationInfo> Handle(ParseRadioStationRequest request, CancellationToken cancellationToken)
    {
        await _myTunerStationInfoScraper.ParseOneStationInfo(request.RadioStation);
        if (string.IsNullOrEmpty(request.RadioStation.StationStreamUrl))
        {
            // Sometimes MyTuner opens a stream in a popup window with some changed URL. Here is another try to get stream URL opening a popup URL.
            await _myTunerStationInfoScraper.ParseOneStationInfo(request.RadioStation, true);
        }
        return request.RadioStation;
    }
}