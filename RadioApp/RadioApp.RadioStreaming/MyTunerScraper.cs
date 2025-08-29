using MediatR;
using Microsoft.Extensions.Logging;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.RadioStreaming.WebScraper;

namespace RadioApp.RadioStreaming;

public class MyTunerScraper: IRequestHandler<GetMyTunerCountriesListRequest, MyTunerCountryInfo[]>
{
    private readonly ILogger<MyTunerScraper> _logger;
    private readonly MyTunerCountriesScrapper  _myTunerCountriesScrapper;

    public MyTunerScraper(ILogger<MyTunerScraper> logger, MyTunerCountriesScrapper myTunerCountriesScrapper)
    {
        _logger = logger;
        _myTunerCountriesScrapper = myTunerCountriesScrapper;
    }

    public Task<MyTunerCountryInfo[]> Handle(GetMyTunerCountriesListRequest request, CancellationToken cancellationToken)
    {
        return _myTunerCountriesScrapper.GetCountries(cancellationToken);
    }
}