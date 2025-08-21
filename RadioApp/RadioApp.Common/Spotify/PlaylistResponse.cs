namespace RadioApp.Common.Spotify;

public class PlaylistResponse
{
    public PlaylistItem[]? Items { get; set; }
}

public class PlaylistItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}