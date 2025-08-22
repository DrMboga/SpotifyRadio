using System.Text.Json;
using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.PlayerProcessor;

namespace RadioApp.PlayerProcessors;

public class SpotifyPlayerProcessor : IPlayerProcessor
{
    private readonly ILogger<SpotifyPlayerProcessor> _logger;
    private readonly IMediator _mediator;

    private bool _canPlay = false;
    private Common.Contracts.SpotifySettings? _spotifySettings = null;
    private int _frequencyValue;

    private bool _newStart = true;
    private string _deviceId = string.Empty;
    private string _playlistId = string.Empty;

    public PlayerType Type => PlayerType.Spotify;

    public SpotifyPlayerProcessor(ILogger<SpotifyPlayerProcessor> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }


    public async Task Start(SabaRadioButtons currentButton, PlayerMode currentPlayerMode, int currentFrequency)
    {
        _logger.LogInformation(
            $"Starting Spotify player processor. Mode: {currentPlayerMode}; Frequency: {currentFrequency}");
        _frequencyValue = currentFrequency;
        _newStart = true;
        await _mediator.Publish(new ClearScreenNotification());
        await _mediator.Publish(new ShowStaticImageNotification("SpotifySabaLogo.bmp", 0));

        _spotifySettings = await _mediator.Send(new GetSpotifySettingsRequest());
        _canPlay = CheckSpotifySettings(_spotifySettings);
        if (!_canPlay)
        {
            _logger.LogDebug($"Spotify settings are incorrect: '{JsonSerializer.Serialize(_spotifySettings)}'");
            await _mediator.Publish(new ShowStaticImageNotification("SpotifyAuthError.bmp", 0));
            return;
        }

        if (currentPlayerMode == PlayerMode.Play)
        {
            await Play();
        }
    }

    public async Task Stop()
    {
        if (!_canPlay)
        {
            return;
        }

        await RefreshTokenIfNeeded();
        var deviceId = await GetCurrentDeviceId();
        if (!_canPlay || string.IsNullOrEmpty(deviceId))
        {
            return;
        }
        var success =
            await _mediator.Send(new PausePlaybackRequest(_spotifySettings!.AuthToken!, deviceId));
        if (!success)
        {
            await _mediator.Publish(new ShowStaticImageNotification("SpotifyApiError.bmp", 0));
            _canPlay = false;
        }
    }

    public async Task Play()
    {
        await RefreshTokenIfNeeded();
        if (!_canPlay)
        {
            return;
        }
        var deviceId = await GetCurrentDeviceId();
        if (string.IsNullOrEmpty(deviceId))
        {
            return;
        }

        var playlistId = await GetCurrentPlaylistId();
        if (string.IsNullOrEmpty(playlistId))
        {
            return;
        }
        _logger.LogDebug($"{(_newStart ? "Playing" : "Resuming")} '{playlistId}' playlist on '{deviceId}' device");
        var success =
            await _mediator.Send(new StartPlaybackRequest(_spotifySettings!.AuthToken!, deviceId, playlistId, !_newStart));
        if (!success)
        {
            await _mediator.Publish(new ShowStaticImageNotification("SpotifyApiError.bmp", 0));
            _canPlay = false;
            return;
        }

        if (_newStart)
        {
            await _mediator.Publish(new ToggleShuffleNotification(_spotifySettings!.AuthToken!, deviceId));
            _newStart = false;
        }
    }

    public Task Pause()
    {
        return Stop();
    }

    public Task ToggleButtonChanged(SabaRadioButtons button)
    {
        return Task.CompletedTask;
    }

    public Task FrequencyChanged(int frequency)
    {
        // TODO: check _canPlay flag
        return Task.CompletedTask;
    }

    private static bool CheckSpotifySettings(Common.Contracts.SpotifySettings spotifySettings)
    {
        return !string.IsNullOrEmpty(spotifySettings.ClientId)
               && !string.IsNullOrEmpty(spotifySettings.ClientSecret)
               && !string.IsNullOrEmpty(spotifySettings.AuthToken)
               && !string.IsNullOrEmpty(spotifySettings.RefreshToken)
               && !string.IsNullOrEmpty(spotifySettings.RedirectUrl)
               && !string.IsNullOrEmpty(spotifySettings.DeviceName)
               && !string.IsNullOrEmpty(spotifySettings.PlaylistName);
    }

    private async Task RefreshTokenIfNeeded()
    {
        if (_spotifySettings == null)
        {
            _canPlay = false;
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tokenExpired = now > _spotifySettings.AuthTokenExpiration;
        if (!tokenExpired)
        {
            // No need to refresh, token is valid
            return;
        }

        // Call Spotify API to refresh token
        var refreshedToken = await _mediator.Send(new RefreshSpotifyAuthTokenRequest(_spotifySettings));
        _logger.LogDebug($"Refreshed Token: {JsonSerializer.Serialize(refreshedToken)}");

        if (string.IsNullOrEmpty(refreshedToken?.AccessToken))
        {
            _logger.LogWarning("AccessToken is empty");
            await _mediator.Publish(new ShowStaticImageNotification("SpotifyApiError.bmp", 0));
            _canPlay = false;
            return;
        }

        // Save refreshed token
        _spotifySettings.AuthToken = refreshedToken.AccessToken;
        // Sometimes API returns an empty refresh token. That means that the old one is still valid
        if (!string.IsNullOrEmpty(refreshedToken.RefreshToken))
        {
            // Save new refresh token if API returns it
            _spotifySettings.RefreshToken = refreshedToken.RefreshToken;
        }
        _spotifySettings.AuthTokenExpiration = now + refreshedToken.ExpiresIn * 1000;

        await _mediator.Publish(new SetSpotifySettingsNotification(_spotifySettings));
    }

    private async Task<string?> GetCurrentDeviceId()
    {
        if (string.IsNullOrEmpty(_spotifySettings?.AuthToken))
        {
            _canPlay = false;
            return null;
        }

        if (!string.IsNullOrEmpty(_deviceId))
        {
            return _deviceId;
        }

        var devices = await _mediator.Send(new GetAvailableDevicesRequest(_spotifySettings.AuthToken));
        var currentDevice = devices.FirstOrDefault(d => d.Name == _spotifySettings.DeviceName);
        if (currentDevice == null)
        {
            _logger.LogWarning($"No available devices found for {_spotifySettings.DeviceName}");
            await _mediator.Publish(new ShowStaticImageNotification("SpotifyApiError.bmp", 0));
            _canPlay = false;
            return null;
        }

        _deviceId =  currentDevice.Id;
        return currentDevice.Id;
    }

    private async Task<string?> GetCurrentPlaylistId()
    {
        if (string.IsNullOrEmpty(_spotifySettings?.AuthToken))
        {
            _canPlay = false;
            return null;
        }

        if (!string.IsNullOrEmpty(_playlistId))
        {
            return _playlistId;
        }

        var playlists = await _mediator.Send(new GetSpotifyPlaylistsRequest(_spotifySettings.AuthToken));
        var currentPlaylist = playlists.FirstOrDefault(p => p.Name == _spotifySettings.PlaylistName);
        if (currentPlaylist == null)
        {
            _logger.LogWarning($"No playlist found for {_spotifySettings.PlaylistName}");
            await _mediator.Publish(new ShowStaticImageNotification("SpotifyApiError.bmp", 0));
            _canPlay = false;
            return null;
        }

        _playlistId = currentPlaylist.Id;
        return currentPlaylist.Id;
    }
}