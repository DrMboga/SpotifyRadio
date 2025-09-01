using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;

namespace RadioApp.RadioStreaming.WebScraper;

public class MyTunerStationsScraper : MyTunerScraperBase
{
    private readonly ILogger<MyTunerStationsScraper> _logger;
    private readonly IMediator _mediator;

    public MyTunerStationsScraper(ILogger<MyTunerStationsScraper> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
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
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get radio station list");
        }
    }

    /// <summary>
    /// Parses one station details (For background processing) 
    /// </summary>
    /// <param name="stationInfo"></param>
    public async Task ParseOneStationInfo(RadioStationInfo stationInfo)
    {
        var stationInfoUrl = NormalizeUrl(stationInfo.DetailsUrl, "https://mytuner-radio.com");
        
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var browserContext = await GenerateBrowserContext(browser);
        var page = await browserContext.NewPageAsync();

        var foundStreamUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        EventHandler<IResponse> responseHandler = async (_, resp) =>
        {
            if (resp.LooksLikeAudio())
            {
                lock (foundStreamUrls) foundStreamUrls.Add(resp.Url);
            }

            await Task.CompletedTask;
        };

        // Page can start to play audio stream on load
        page.Response += responseHandler;

        try
        {
            // Navigate to page
            await page.GotoAsync(stationInfoUrl,
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });
            _logger.LogDebug($"Navigation start: '{stationInfoUrl}'");

            // Parse page
            var radioImageElement = await page.QuerySelectorAsync(".radio-image");
            if (radioImageElement != null)
            {
                stationInfo.StationImageUrl = await radioImageElement.GetAttributeAsync("src") ??
                                              await radioImageElement.GetAttributeAsync("data-src");
            }

            // Play
            var stationPlayButton = await page.QuerySelectorAsync("#play-button");
            if (stationPlayButton != null)
            {
                var playButtonStyle = await stationPlayButton.GetAttributeAsync("style");
                // If style is "display: none;" then, pause button is shown that means that stream is already playing
                if (playButtonStyle != null && !playButtonStyle.Contains("display: none;"))
                {
                    try
                    {
                        // Click to play, then stream URL should be caught in the Response handler
                        await stationPlayButton.ClickAsync();
                    }
                    catch (TimeoutException exception)
                    {
                        _logger.LogError(exception, $"Failed to click play button '{stationInfoUrl}'");
                    }
                }
            }

            // Like/dislike
            var likeButton = await page.QuerySelectorAsync("#like_button");
            var dislikeButton = await page.QuerySelectorAsync("#dislike_button");
            stationInfo.Likes = await GetSpanTextAsInt(likeButton);
            stationInfo.Dislikes = await GetSpanTextAsInt(dislikeButton);

            // Description
            var descriptionElement = await page.QuerySelectorAsync(".description");
            if (descriptionElement != null)
            {
                var pElement = await descriptionElement.QuerySelectorAsync("p");
                if (pElement != null)
                {
                    stationInfo.StationDescription = await pElement.InnerTextAsync();
                }
            }

            // Rating
            var ratingElement = await page.QuerySelectorAsync("#yellow_stars");
            stationInfo.Rating = await GetRatingInPercent(ratingElement);

            // Station web page
            var contactsElement = await page.QuerySelectorAsync(".contacts");
            if (contactsElement != null)
            {
                var anchor = await contactsElement.QuerySelectorAsync("a");
                if (anchor != null)
                {
                    stationInfo.StationWebPage = await anchor.GetAttributeAsync("href");
                }
            }


            // Wait for network requests to grab an audio stream if page starts to play radio on load
            await page.WaitForTimeoutAsync(7000);
            stationInfo.StationStreamUrl = await foundStreamUrls.PickPreferredStreamUrl();
            
            // TODO: Save one station info to DB With StationProcessed status = true
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to get radio station info: '{stationInfoUrl}'");
        }
        finally
        {
            page.Response -= responseHandler;
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

    private static string NormalizeUrl(string url, string baseUrl)
    {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;

        return new Uri(new Uri(baseUrl), url).ToString();
    }

    private async Task<int> GetSpanTextAsInt(IElementHandle? element)
    {
        if (element == null)
        {
            return 0;
        }

        var spanElement = await element.QuerySelectorAsync("span");
        if (spanElement == null)
        {
            return 0;
        }

        var spanText = await spanElement.InnerTextAsync();

        if (int.TryParse(spanText, out var spanValue))
        {
            return spanValue;
        }

        return 0;
    }

    /// <summary>
    /// Parses attribute which looks like: style="width: 75%;"
    /// </summary>
    private async Task<int> GetRatingInPercent(IElementHandle? element)
    {
        if (element == null)
        {
            return 0;
        }

        var style = await element.GetAttributeAsync("style");
        if (style == null)
        {
            return 0;
        }

        return int.TryParse(System.Text.RegularExpressions.Regex.Match(style, @"\d+").Value, out var value) ? value : 0;
    }
}