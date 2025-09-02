using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;
using RadioApp.Common.MyTunerScraper;

namespace RadioApp.RadioStreaming.WebScraper;

public class MyTunerStationsScraper : MyTunerScraperBase
{
    private readonly ILogger<MyTunerStationsScraper> _logger;
    private readonly IMediator _mediator;
    private readonly MyTunerCachingDispatcher _myTunerCachingDispatcher;

    public MyTunerStationsScraper(ILogger<MyTunerStationsScraper> logger, IMediator mediator,
        MyTunerCachingDispatcher myTunerCachingDispatcher)
    {
        _logger = logger;
        _mediator = mediator;
        _myTunerCachingDispatcher = myTunerCachingDispatcher;
    }

    /// <summary>
    /// Gets a list of stations from MyTuner by country and saves the full list to the DB with empty station info
    /// </summary>
    public async Task StartCachingStations(string country, string countryUrl)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var browserContext = await GenerateBrowserContext(browser);
        var page = await browserContext.NewPageAsync();

        try
        {
            var stationsCatalog = await GetStationsList(country, countryUrl, page);

            _logger.LogDebug($"{stationsCatalog.Length} stations found");
            await _mediator.Publish(new SaveStationsInfosNotification(stationsCatalog));
            _myTunerCachingDispatcher.SignalForStartProcessing();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get radio station list");
        }
    }

    /// <summary>
    /// Parses a stations list by country
    /// </summary>
    private async Task<RadioStationInfo[]> GetStationsList(string country, string countryUrl,
        IPage page)
    {
        var stationsCatalog = new Dictionary<string, RadioStationInfo>();

        await page.GotoAsync(countryUrl,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });
        var statesContainer = await page.QuerySelectorAsync(".states");
        if (statesContainer == null)
        {
            return [];
        }

        var children = await statesContainer.QuerySelectorAllAsync(":scope > *");
        string lastState = string.Empty;

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var tagProp = await child.GetPropertyAsync("tagName");
            var tagName = (await tagProp.JsonValueAsync<string>())?.ToUpperInvariant();

            if (tagName == "H2")
            {
                // Supposed to be a state name
                lastState = (await child.InnerTextAsync())?.Trim() ?? string.Empty;
            }

            if (tagName == "DIV")
            {
                // Information about region, there is an ul-li list with stations
                var listItems = await child.QuerySelectorAllAsync("li");
                foreach (var listItem in listItems)
                {
                    var fullText = (await listItem.InnerTextAsync())?.Trim() ?? string.Empty;
                    var anchor = await listItem.QuerySelectorAsync("a");
                    if (anchor != null)
                    {
                        // Anchor parts
                        var stationHref = await anchor.GetAttributeAsync("href");
                        var stationName = (await anchor.InnerTextAsync()).Trim();
                        var stationFrequency = fullText.Replace(stationName, "").Trim();

                        var regionInfo = string.IsNullOrEmpty(lastState)
                            ? stationFrequency
                            : $"{stationFrequency} in {lastState}";

                        if (string.IsNullOrEmpty(stationHref))
                        {
                            continue;
                        }

                        if (stationsCatalog.ContainsKey(stationHref))
                        {
                            stationsCatalog[stationHref].RegionInfo =
                                $"{stationsCatalog[stationHref].RegionInfo} | {regionInfo}";
                        }
                        else
                        {
                            stationsCatalog.Add(stationHref, new RadioStationInfo()
                                {
                                    Name = stationName,
                                    DetailsUrl = stationHref,
                                    Country = country,
                                    RegionInfo = regionInfo,
                                }
                            );
                        }
                    }
                }
            }
        }

        return stationsCatalog.Values.ToArray();
    }
}