using MediatR;
using Microsoft.Extensions.Logging;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.RadioStreaming.WebScraper;

namespace RadioApp.RadioStreaming;

public class MyTunerScraper: 
    IRequestHandler<GetMyTunerCountriesListRequest, MyTunerCountryInfo[]>,
    INotificationHandler<StartCacheMyTunerStationsByCountryNotification>,
    IRequestHandler<ParseRadioStationRequest, RadioStationInfo>
{
    private readonly ILogger<MyTunerScraper> _logger;
    private readonly MyTunerCountriesScrapper  _myTunerCountriesScrapper;
    private readonly MyTunerStationsScraper   _myTunerStationsScraper;

    public MyTunerScraper(ILogger<MyTunerScraper> logger, MyTunerCountriesScrapper myTunerCountriesScrapper, MyTunerStationsScraper myTunerStationsScraper)
    {
        _logger = logger;
        _myTunerCountriesScrapper = myTunerCountriesScrapper;
        _myTunerStationsScraper = myTunerStationsScraper;
    }

    public Task<MyTunerCountryInfo[]> Handle(GetMyTunerCountriesListRequest request, CancellationToken cancellationToken)
    {
        return _myTunerCountriesScrapper.GetCountries(cancellationToken);
    }

    public Task Handle(StartCacheMyTunerStationsByCountryNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Radio station caching for '{notification.CountryInfo.Country}': '{notification.CountryInfo.Url}'");
        return _myTunerStationsScraper.StartCachingStations(notification.CountryInfo.Country, notification.CountryInfo.Url);
    }

    public async Task<RadioStationInfo> Handle(ParseRadioStationRequest request, CancellationToken cancellationToken)
    {
        await _myTunerStationsScraper.ParseOneStationInfo(request.RadioStation);
        return request.RadioStation;
    }
}