using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RadioApp.Common.Contracts;

namespace RadioApp.RadioStreaming.WebScraper;

public class MyTunerStationsScraper : MyTunerScraperBase
{
    private readonly ILogger<MyTunerStationsScraper> _logger;

    public MyTunerStationsScraper(ILogger<MyTunerStationsScraper> logger)
    {
        _logger = logger;
    }

    public async Task<RadioStationInfo[]> GetStations(string country, string countryUrl)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var browserContext = await GenerateBrowserContext(browser);
        var page = await browser.NewPageAsync();

        var result = new List<RadioStationInfo>();

        try
        {
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
                            
                            _logger.LogDebug($"'{stationName}': {stationFrequency} in {lastState} | '{stationHref}'");
                        }
                    }
                }
            }
            
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get radio station list");
        }

        return [];
    }
}