using System.Text.RegularExpressions;
using Microsoft.Playwright;
using RadioApp.RadioStreaming.Model;

namespace RadioApp.RadioStreaming.WebScraper;

public static class AudioLinksHelper
{
    private static readonly Regex UrlHint = new(
        @"(\.m3u8(\?.*)?$)|(\.mp3(\?.*)?$)|(\.aac(\?.*)?$)|(/icecast/|/shoutcast/|/stream)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool LooksLikeAudio(this IResponse r) =>
        (r.Headers.TryGetValue("content-type", out var ct) &&
         (ct.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
          || ct.Contains("mpegurl", StringComparison.OrdinalIgnoreCase)
          || ct.Contains("x-mpegurl", StringComparison.OrdinalIgnoreCase)))
        || UrlHint.IsMatch(r.Url);

    public static async Task<string?> PickPreferredStreamUrl(this HashSet<string> foundStreamUrls)
    {
        var validated = new List<StreamInfo>();
        foreach (var foundUrl in foundStreamUrls)
        {
            var expanded = await ExpandIfPlaylistAsync(foundUrl);
            foreach (var s in expanded)
            {
                validated.Add(s);
            }
        }
        
        return validated.Count > 0 ? PickPreferred(validated)?.Url : null;
    }
    
    private static async Task<List<StreamInfo>> ExpandIfPlaylistAsync(string url)
    {
        var lower = url.ToLowerInvariant();
        if (!(lower.EndsWith(".m3u") || lower.EndsWith(".m3u8") || lower.EndsWith(".pls")))
            return new() { new StreamInfo(url, FormatGuess(url)) };

        using var http = new HttpClient();
        var text = await http.GetStringAsync(url);

        if (lower.EndsWith(".pls"))
        {
            // simple PLS
            var list = new List<StreamInfo>();
            foreach (var line in text.Split('\n'))
                if (line.TrimStart().StartsWith("File", StringComparison.OrdinalIgnoreCase))
                    list.Add(new StreamInfo(line.Split('=')[1].Trim(), FormatGuess(line)));
            return list;
        }

        if (lower.EndsWith(".m3u") || lower.EndsWith(".m3u8"))
        {
            // M3U/M3U8: gather all http(s) strings
            var list = new List<StreamInfo>();
            foreach (var line in text.Split('\n').Select(l => l.Trim()))
                if (line.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    list.Add(new StreamInfo(line, lower.EndsWith(".m3u8") ? "hls" : FormatGuess(line)));
            return list.Count > 0 ? list : new() { new StreamInfo(url, "hls") };
        }

        return new() { new StreamInfo(url, FormatGuess(url)) };
    }

    private static StreamInfo PickPreferred(List<StreamInfo> items)
    {
        var best = items
            .OrderByDescending(s => s.Format is "mp3" or "aac")
            .ThenBy(s => s.Format == "hls")
            .ThenByDescending(s => s.BitrateKbps ?? 0)
            .First();
        return best!;
    }
    
    private static string? FormatGuess(string s)
    {
        s = s.ToLowerInvariant();
        if (s.Contains(".m3u8")) return "hls";
        if (s.Contains(".mp3")) return "mp3";
        if (s.Contains(".aac")) return "aac";
        if (s.EndsWith(".pls")) return "pls";
        if (s.EndsWith(".m3u")) return "m3u";
        return null;
    }
}