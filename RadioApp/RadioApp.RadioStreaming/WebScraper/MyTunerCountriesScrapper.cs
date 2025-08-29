using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RadioApp.Common.Contracts;

namespace RadioApp.RadioStreaming.WebScraper;

public class MyTunerCountriesScrapper : MyTunerScraperBase
{
    private const string MyTunerCountriesUrl = "https://mytuner-radio.com/radio/worldwide-frequencies/";
    private readonly ILogger<MyTunerCountriesScrapper> _logger;

    public MyTunerCountriesScrapper(ILogger<MyTunerCountriesScrapper> logger)
    {
        _logger = logger;
    }

    public async Task<MyTunerCountryInfo[]> GetCountries(CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var browserContext = await GenerateBrowserContext(browser);
        var page = await browser.NewPageAsync();

        var result = new List<MyTunerCountryInfo>();

        try
        {
            await page.GotoAsync(MyTunerCountriesUrl,
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });

            var countries = await page.EvaluateAsync(CountriesScrapScript);

            foreach (var countryElement in countries?.EnumerateArray())
            {
                result.Add(new MyTunerCountryInfo(
                    countryElement.GetProperty("countryName").GetString() ?? string.Empty,
                    countryElement.GetProperty("imageUrl").GetString() ?? string.Empty,
                    countryElement.GetProperty("url").GetString() ?? string.Empty)
                );
            }
            
            return [..result.OrderBy(c => c.Country)];
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error scraping countries");
        }

        return [];
    }

    private const string CountriesScrapScript = @"
(() => {

    const countries = new Set();

    const parseCountyElement = (el) => {
        const countryName = el.querySelector('span')?.innerText;
        const imageUrl = el.querySelector('img')?.getAttribute('src') ?? el.querySelector('img')?.getAttribute('data-src');
        const url = el.querySelector(""a[href*='fm']"")?.href;
        countries.add(({countryName, imageUrl, url}));
    };

    document.querySelector('.continents').querySelectorAll('li')
        .forEach(el => {parseCountyElement(el);});

    return Array.from(countries);
})()";
}