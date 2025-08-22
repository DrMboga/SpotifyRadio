using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MediatR;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.Spotify;

namespace RadioApp.SpotifySettings;

public class SpotifyApi :
    IRequestHandler<RefreshSpotifyAuthTokenRequest, RefreshTokenResponse>,
    IRequestHandler<GetAvailableDevicesRequest, AvailableDevicesResponse[]>,
    IRequestHandler<GetSpotifyPlaylistsRequest, PlaylistItem[]>,
    IRequestHandler<StartPlaybackRequest, bool>,
    INotificationHandler<ToggleShuffleNotification>,
    IRequestHandler<PausePlaybackRequest, bool>,
    IRequestHandler<SkipSongRequest, bool>,
    IRequestHandler<GetCurrentlyPlayingInfoRequest, SongInfoResponse?>
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
        const string url = "https://accounts.spotify.com/api/token";

        if (string.IsNullOrEmpty(request.SpotifySettings?.ClientId) ||
            string.IsNullOrEmpty(request.SpotifySettings?.ClientSecret) ||
            string.IsNullOrEmpty(request.SpotifySettings?.RefreshToken))
        {
            _logger.LogWarning(
                $"Unable to refresh Spotify Token, invalid settings '{JsonSerializer.Serialize(request.SpotifySettings)}'");
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

            using var response = await client.SendAsync(httpRequest, cancellationToken);
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

    public async Task<AvailableDevicesResponse[]> Handle(GetAvailableDevicesRequest request,
        CancellationToken cancellationToken)
    {
        // API Call example https://developer.spotify.com/documentation/web-api/reference/get-a-users-available-devices
        const string url = "https://api.spotify.com/v1/me/player/devices";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AuthToken);

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<AvailableDevicesApiResponse>(
                    cancellationToken: cancellationToken);
            return result?.Devices ?? [];
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting available devices");
        }

        return [];
    }

    public async Task<PlaylistItem[]> Handle(GetSpotifyPlaylistsRequest request, CancellationToken cancellationToken)
    {
        // API Call example https://developer.spotify.com/documentation/web-api/reference/get-a-list-of-current-users-playlists
        const string url = "https://api.spotify.com/v1/me/playlists";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AuthToken);

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<PlaylistResponse>(cancellationToken: cancellationToken);
            return result?.Items ?? [];
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting playlists");
        }

        return [];
    }

    public async Task<bool> Handle(StartPlaybackRequest request, CancellationToken cancellationToken)
    {
        // API call example https://developer.spotify.com/documentation/web-api/reference/start-a-users-playback
        const string url = "https://api.spotify.com/v1/me/player/play";
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AuthToken);

            using StringContent jsonContent = new(
                JsonSerializer.Serialize(new
                {
                    context_uri = $"spotify:playlist:{request.PlaylistId}",
                    position_ms = 0
                }),
                Encoding.UTF8,
                "application/json");
            using var response = await client.PutAsync(
                $"{url}?device_id={request.DeviceId}",
                request.Resume ? new StringContent(string.Empty) : jsonContent,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error starting playback");
        }

        return false;
    }

    public async Task Handle(ToggleShuffleNotification notification, CancellationToken cancellationToken)
    {
        // API call example https://developer.spotify.com/documentation/web-api/reference/toggle-shuffle-for-users-playback
        const string url = "https://api.spotify.com/v1/me/player/shuffle?state=true";
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", notification.AuthToken);

            using var response = await client.PutAsync($"{url}&device_id={notification.DeviceId}",
                new StringContent(string.Empty), cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error toggle shuffle");
        }
    }

    public async Task<bool> Handle(PausePlaybackRequest request, CancellationToken cancellationToken)
    {
        // API call example https://developer.spotify.com/documentation/web-api/reference/pause-a-users-playback
        const string url = "https://api.spotify.com/v1/me/player/pause";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", request.AuthToken);

            using var response = await client.PutAsync($"{url}?device_id={request.DeviceId}",
                new StringContent(string.Empty), cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error pause playback");
        }

        return false;
    }

    public async Task<bool> Handle(SkipSongRequest request, CancellationToken cancellationToken)
    {
        // API call examples: https://developer.spotify.com/documentation/web-api/reference/skip-users-playback-to-next-track
        // https://developer.spotify.com/documentation/web-api/reference/skip-users-playback-to-previous-track
        const string url = "https://api.spotify.com/v1/me/player/";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", request.AuthToken);

            using var response = await client.PostAsync(
                $"{url}{(request.SkipToNext ? "next" : "previous")}?device_id={request.DeviceId}",
                new StringContent(string.Empty), cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error skip song");
        }

        return false;
    }

    public async Task<SongInfoResponse?> Handle(GetCurrentlyPlayingInfoRequest request,
        CancellationToken cancellationToken)
    {
        // API call example: https://developer.spotify.com/documentation/web-api/reference/get-the-users-currently-playing-track
        const string url = "https://api.spotify.com/v1/me/player/currently-playing";

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", request.AuthToken);
            using var response = await client.GetAsync($"{url}?market={request.Market}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<SongInfoResponse>(content, JsonSerializerOptions.Web);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting current playing info");
        }

        return null;
    }
}