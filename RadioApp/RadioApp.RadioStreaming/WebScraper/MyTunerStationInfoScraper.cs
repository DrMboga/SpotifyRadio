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
    /// <param name="embed"></param>
    public async Task ParseOneStationInfo(RadioStationInfo stationInfo, bool embed = false)
    {
        var stationInfoUrl = embed
            ? BuildEmbedUrl(stationInfo.DetailsUrl, "http://e.mytuner-radio.com/embed")
            : NormalizeUrl(stationInfo.DetailsUrl, "https://mytuner-radio.com");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = new[]
            {
                "--disable-popup-blocking",
                "--autoplay-policy=no-user-gesture-required"
            }
        });
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

            await TryAcceptConsent(page);

            // Play
            await ClickPlayButton(page, stationInfoUrl);

            // Parse page
            if (!embed)
            {
                stationInfo.StationImageUrl = await page.ParseStationImage();
                stationInfo.Likes = await page.GetLikesOrDislikes("#like_button");
                stationInfo.Dislikes = await page.GetLikesOrDislikes("#dislike_button");
                stationInfo.StationDescription = await page.ParseDescription();
                stationInfo.Rating = await page.GetRatingInPercent();
                stationInfo.StationWebPage = await page.ParseStationWebPage();
            }

            // Wait for network requests to grab an audio stream if page starts to play radio on load
            await page.WaitForTimeoutAsync(3000);
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

    private async Task TryAcceptConsent(IPage page)
    {
        var consentRoot = page.Locator("#qc-cmp2-ui");
        if (await consentRoot.IsVisibleAsync(new() { Timeout = 1000 }))
        {
            _logger.LogInformation("Got consent response");
            var btn = page.GetByRole(AriaRole.Button, new()
            {
                Name = "AGREE"
            });

            if (await btn.IsVisibleAsync())
            {
                _logger.LogInformation("Agree button is visible");
                await btn.ScrollIntoViewIfNeededAsync();
                await btn.ClickAsync(new() { Timeout = 15000 });
                await consentRoot.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 15000 });
            }
        }
    }

    private async Task ClickPlayButton(IPage page, string stationInfoUrl)
    {
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
    }

    /// <summary>
    /// Checks if URL is relative, adds a base address
    /// </summary>
    private static string NormalizeUrl(string url, string baseUrl)
    {
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;

        return new Uri(new Uri(baseUrl), url).ToString();
    }

    private static string BuildEmbedUrl(string relativeUrl, string baseUrl)
    {
        var slug = relativeUrl.TrimEnd('/').Split('/').Last();
        return $"{baseUrl}/{slug}";
    }
}

/*
 * <button mode="primary" size="large" class=" css-47sehv"><span>AGREE</span></button>
 *
 * http://e.mytuner-radio.com/embed/1046-rtl-die-besten-neuen-hits-444306
 */