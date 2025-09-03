using Microsoft.Playwright;

namespace RadioApp.RadioStreaming.WebScraper;

internal static class MyTunerStationInfoHelper
{
    public static async Task<string?> ParseStationImage(this IPage page)
    {
        var radioImageElement = await page.QuerySelectorAsync(".radio-image");
        if (radioImageElement != null)
        {
            return await radioImageElement.GetAttributeAsync("src") ??
                   await radioImageElement.GetAttributeAsync("data-src");
        }

        return null;
    }
    
    public static async Task<string> ParseDescription(this IPage page)
    {
        var descriptionElement = await page.QuerySelectorAsync(".description");
        if (descriptionElement != null)
        {
            var pElement = await descriptionElement.QuerySelectorAsync("p");
            if (pElement != null)
            {
                return await pElement.InnerTextAsync();
            }

            return await descriptionElement.InnerTextAsync();
        }
        return string.Empty;
    }

    public static async Task<string?> ParseStationWebPage(this IPage page)
    {
        var contactsElement = await page.QuerySelectorAsync(".contacts");
        if (contactsElement != null)
        {
            var anchor = await contactsElement.QuerySelectorAsync("a");
            if (anchor != null)
            {
                return await anchor.GetAttributeAsync("href");
            }
        }
        return null;
    }
    
    
    public static async Task<int> GetLikesOrDislikes(this IPage page, string buttonId)
    {
        var buttonElement = await page.QuerySelectorAsync(buttonId);
        
        if (buttonElement == null)
        {
            return 0;
        }

        var spanElement = await buttonElement.QuerySelectorAsync("span");
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
    public static async Task<int> GetRatingInPercent(this IPage page)
    {
        var ratingElement = await page.QuerySelectorAsync("#yellow_stars");
        if (ratingElement == null)
        {
            return 0;
        }

        var style = await ratingElement.GetAttributeAsync("style");
        if (style == null)
        {
            return 0;
        }

        return int.TryParse(System.Text.RegularExpressions.Regex.Match(style, @"\d+").Value, out var value) ? value : 0;
    }

    /// <summary>
    /// Parses Genres chips
    /// </summary>
    public static async Task<string?> ParseGenres(this IPage page)
    {
        var genresPanel = await page.QuerySelectorAsync(".genres");
        if (genresPanel == null)
        {
            return null;
        }
        
        var allAnchors = await genresPanel.QuerySelectorAllAsync("a");

        string result = string.Empty;
        foreach (var anchor in allAnchors)
        {
            var genre = await anchor.InnerTextAsync();
            if (!string.IsNullOrWhiteSpace(genre))
            {
                result = $"{(string.IsNullOrWhiteSpace(result) ? genre : $"{result} | {genre}")}";
            }
        }
        
        return result;
    }

}