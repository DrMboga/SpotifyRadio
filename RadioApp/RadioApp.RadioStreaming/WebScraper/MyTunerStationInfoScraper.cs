using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using RadioApp.Common.Contracts;

namespace RadioApp.RadioStreaming.WebScraper;

public class MyTunerStationInfoScraper : MyTunerScraperBase
{
    private readonly ILogger<MyTunerStationInfoScraper> _logger;

    public MyTunerStationInfoScraper(ILogger<MyTunerStationInfoScraper> logger)
    {
        _logger = logger;
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
                lock (foundStreamUrls)
                {
                    foundStreamUrls.Add(resp.Url);
                }
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
                if (string.IsNullOrEmpty(playButtonStyle) || !playButtonStyle.Contains("display: none;"))
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
                else
                {
                    stationInfo.StationDescription = await descriptionElement.InnerTextAsync();
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
            if (string.IsNullOrEmpty(stationInfo.StationStreamUrl))
            {
                _logger.LogWarning($"Not found audio stream for '{stationInfoUrl}'");
            }
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