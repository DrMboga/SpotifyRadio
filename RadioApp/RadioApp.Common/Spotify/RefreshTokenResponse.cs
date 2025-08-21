using System.Text.Json.Serialization;

namespace RadioApp.Common.Spotify;

public class RefreshTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } =  string.Empty;
}