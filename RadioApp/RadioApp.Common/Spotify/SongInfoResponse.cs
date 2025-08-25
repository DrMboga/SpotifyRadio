using System.Text.Json.Serialization;

namespace RadioApp.Common.Spotify;

public class SongInfoResponse
{
    [JsonPropertyName("progress_ms")]
    public int Progress { get; set;}
    [JsonPropertyName("is_playing")]
    public bool IsPlaying { get; set;}
    public SongItem? Item { get; set; }
}

public class SongItem
{
    public AlbumInfo? Album { get; set; }
    public ArtistInfo[]? Artists { get; set; }
    [JsonPropertyName("duration_ms")]
    public int Duration { get; set; }
    public string? Name { get; set; }
    public string? Id { get; set; }
    public string? Uri { get; set; }
}

public class AlbumInfo
{
    public AlbumImage[]? Images { get; set; }
    public string? Name { get; set; }
}

public class AlbumImage
{
    public string? Url { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}

public class ArtistInfo
{
    public string? Name { get; set; }
}