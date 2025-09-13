using Microsoft.Extensions.DependencyInjection;
using RadioApp.Common.MyTunerScraper;
using RadioApp.RadioStreaming.WebScraper;

namespace RadioApp.RadioStreaming;

public static class RadioStreamingDiSetup
{
    public static void Setup(IServiceCollection services)
    {
        services.AddTransient<MyTunerCountriesScrapper>();
        services.AddTransient<MyTunerStationsScraper>();
        services.AddTransient<MyTunerStationInfoScraper>();
        services.AddSingleton<IRadioVlcPlayer, RadioVlcPlayer>();
    }
}