namespace RadioApp.Common.Contracts;

public class SpotifySettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUrl { get; set; }
    public string? AuthToken { get; set; }
    public long? AuthTokenExpiration { get; set; }
    public string? RefreshToken { get; set; }
    public string? DeviceName { get; set; }
    public string? PlaylistName { get; set; }
}