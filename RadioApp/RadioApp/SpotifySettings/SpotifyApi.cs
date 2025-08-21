using System.Net.Http.Headers;
using System.Text;
using MediatR;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.Spotify;

namespace RadioApp.SpotifySettings;

public class SpotifyApi : 
    IRequestHandler<RefreshSpotifyAuthTokenRequest, RefreshTokenResponse>,
    IRequestHandler<GetAvailableDevicesRequest, AvailableDevicesResponse[]>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SpotifyApi> _logger;

    public SpotifyApi(IHttpClientFactory httpClientFactory, ILogger<SpotifyApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshSpotifyAuthTokenRequest request,
        CancellationToken cancellationToken)
    {
        // API Call example: https://developer.spotify.com/documentation/web-api/tutorials/refreshing-tokens
        _logger.LogDebug("Refreshing Spotify Token");
        var url = "https://accounts.spotify.com/api/token";

        if (string.IsNullOrEmpty(request.SpotifySettings?.ClientId) ||
            string.IsNullOrEmpty(request.SpotifySettings?.ClientSecret) ||
            string.IsNullOrEmpty(request.SpotifySettings?.RefreshToken))
        {
            _logger.LogWarning("Unable to refresh Spotify Token, invalid settings");
            return new RefreshTokenResponse();
        }

        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

            // Build Authorization header (Basic base64(client_id:client_secret))
            var authHeader =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{request.SpotifySettings.ClientId}:{request.SpotifySettings.ClientSecret}"));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            // Add form-urlencoded body
            var body = new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", request.SpotifySettings.RefreshToken),
            };
            httpRequest.Content = new FormUrlEncodedContent(body);

            using var client = _httpClientFactory.CreateClient();

            var response = await client.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var refreshTokenResponse =
                await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: cancellationToken);
            return refreshTokenResponse ?? new RefreshTokenResponse();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while refreshing Spotify Token");
        }

        return new RefreshTokenResponse();
    }

    public async Task<AvailableDevicesResponse[]> Handle(GetAvailableDevicesRequest request, CancellationToken cancellationToken)
    {
        // API Call example https://developer.spotify.com/documentation/web-api/reference/get-a-users-available-devices
        var url = "https://api.spotify.com/v1/me/player/devices";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AuthToken);

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<AvailableDevicesApiResponse>(cancellationToken: cancellationToken);
            return result?.Devices ?? [];
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting available devices");
        }

        return [];
    }
}