using MediatR;
using Microsoft.Extensions.Logging;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.RadioStreaming.WebScraper;

namespace RadioApp.RadioStreaming;

public class MyTunerScraper: 
    IRequestHandler<GetMyTunerCountriesListRequest, MyTunerCountryInfo[]>,
    IRequestHandler<GetMyTunerStationsRequest, RadioStationInfo[]>
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

    public Task<RadioStationInfo[]> Handle(GetMyTunerStationsRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Radio station scraping for '{request.CountryInfo.Country}': '{request.CountryInfo.Url}'");
        return _myTunerStationsScraper.GetStations(request.CountryInfo.Country, request.CountryInfo.Url);
    }
}