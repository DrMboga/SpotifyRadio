using System.Text.Json;
using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.Messages.Spotify;
using RadioApp.Common.PlayerProcessor;
using RadioApp.Common.Spotify;

namespace RadioApp.PlayerProcessors;

public class SpotifyPlayerProcessor : IPlayerProcessor
{
    // Debounce frequency change 
    private const int FrequencyChangeThresholdMilliseconds = 500;

    private readonly ILogger<SpotifyPlayerProcessor> _logger;
    private readonly IMediator _mediator;

    private bool _canPlay = false;
    private bool _isPlaying = false;
    private Common.Contracts.SpotifySettings? _spotifySettings = null;
    private int _frequencyValue;
    private int _temporaryFrequencyValue;
    private long _lastFrequencyChangeTime = 0;
    private readonly Lock _frequencyLock = new Lock();

    private bool _newStart = true;
    private string _deviceId = string.Empty;
    private string _playlistId = string.Empty;

    public PlayerType Type => PlayerType.Spotify;

    private static readonly TimeSpan UpdateSongInterval = TimeSpan.FromSeconds(2);
    private readonly PlayerProcessorTimerService _updateSongTimer;
    private string? _lastSongId = null;

    public SpotifyPlayerProcessor(ILogger<SpotifyPlayerProcessor> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        _updateSongTimer = new PlayerProcessorTimerService(UpdateSongInterval, UpdateSongInfoIfNeeded);
    }


    public async Task Start(SabaRadioButtons currentButton, PlayerMode currentPlayerMode, int currentFrequency)
    {
        _logger.LogInformation(
            $"Starting Spotify player processor. Mode: {currentPlayerMode}; Frequency: {currentFrequency}");
        _frequencyValue = currentFrequency;
        _temporaryFrequencyValue = currentFrequency;
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
        if (!_canPlay || !_isPlaying)
        {
            return;
        }

        _isPlaying = false;
        await _updateSongTimer.Stop();

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
            await _mediator.Send(new StartPlaybackRequest(_spotifySettings!.AuthToken!, deviceId, playlistId,
                !_newStart));
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

        _isPlaying = true;
        await _updateSongTimer.Start();
    }

    public Task Pause()
    {
        return Stop();
    }

    public Task ToggleButtonChanged(SabaRadioButtons button)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// To skip a track we are going to use the radio wave tuning knob
    /// But there is a limit on the tuning scale.
    /// To make the skipping process infinite, there is a trick.
    /// To skip to a next track, we should turn a knob to the right and return it back where it was. Only after returning, the song will be skipped
    /// The same for skipping back, but turning knob to the left for one position and back 
    /// </summary>
    public async Task FrequencyChanged(int frequency)
    {
        lock (_frequencyLock)
        {
            // The capacitance measurement is not precise. So if the knob is somewhere on frequency measurement borders, it can send new frequency values very often.
            // So, we are debouncing this buzz here
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - _lastFrequencyChangeTime < FrequencyChangeThresholdMilliseconds)
            {
                return;
            }

            _lastFrequencyChangeTime = now;
        }

        if (!_canPlay || !_isPlaying)
        {
            return;
        }

        if (_frequencyValue == _temporaryFrequencyValue)
        {
            // First move of the knob
            _temporaryFrequencyValue = frequency;
            _logger.LogDebug($"First move of the knob from {_frequencyValue} to {_temporaryFrequencyValue}");
        }
        else if (_frequencyValue == frequency)
        {
            // Second move, knob returned to original position
            bool skipNext = _temporaryFrequencyValue > frequency;
            _logger.LogDebug(
                $"Knob returned from {_temporaryFrequencyValue} to {_frequencyValue}. Skip song to {(skipNext ? "next" : "previous")}");
            _temporaryFrequencyValue = _frequencyValue;
            // Call API
            await RefreshTokenIfNeeded();
            var deviceId = await GetCurrentDeviceId();
            if (string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            var success = await _mediator.Send(new SkipSongRequest(_spotifySettings.AuthToken, deviceId, skipNext));
            if (!success)
            {
                await _mediator.Publish(new ShowStaticImageNotification("SpotifyApiError.bmp", 0));
                _canPlay = false;
            }
        }
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

        _deviceId = currentDevice.Id;
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

    /// <summary>
    /// This method runs every interval to get song info and show it on TFT
    /// </summary>
    private async Task UpdateSongInfoIfNeeded(CancellationToken cancellationToken)
    {
        var songIno = await GetCurrentSongInfo();
        if (songIno?.Item?.Id == null || songIno?.Item?.Name == null)
        {
            // Show logo if there are no song info
            await _mediator.Publish(new ShowStaticImageNotification("SpotifySabaLogo.bmp", 0), cancellationToken);
            _lastSongId = null;
            return;
        }

        if (string.IsNullOrEmpty(_lastSongId) || songIno.Item.Id != _lastSongId)
        {
            _lastSongId = songIno.Item.Id;

            var image300By300Url = songIno.Item.Album?.Images?.FirstOrDefault(i => i.Height == 300);
            byte[]? albumCoverJpeg = null;
            if (!string.IsNullOrEmpty(image300By300Url?.Url))
            {
                albumCoverJpeg = await _mediator.Send(new GetJpegImageRequest(image300By300Url.Url), cancellationToken);
            }

            // Update picture and song
            var success = await _mediator.Send(new ShowSongInfoRequest(songIno.Item.Name,
                songIno.Item.Artists?.FirstOrDefault()?.Name, albumCoverJpeg), cancellationToken);
            if (!success)
            {
                await _mediator.Publish(new ShowStaticImageNotification("SpotifyApiError.bmp", 0), cancellationToken);
                return;
            }
        }

        int percentage = 0;
        if (songIno.Progress != 0 && songIno.Item.Duration != 0)
        {
            percentage = 100 * songIno.Progress / songIno.Item.Duration;
        }

        // Update position
        await _mediator.Publish(new ShowProgressNotification(percentage), cancellationToken);

        // TODO: Implement ShowSongInfoRequest and ShowProgressNotification in the "DisplayManager". Example: https://github.com/DrMboga/radio/blob/main/dotnetbmpconverter/DotnetBmpConverter/IoCommandsListener.cs
        // TODO: Write tests for this particular logic with timeout 4.01 seconds - to check the ShowSongInfoRequest for 2 different song infos and not calling ShowSongInfoRequest for 2 similar songs. And test ShowProgressNotification call
    }

    private async Task<SongInfoResponse?> GetCurrentSongInfo()
    {
        await RefreshTokenIfNeeded();
        if (string.IsNullOrEmpty(_spotifySettings?.AuthToken))
        {
            _canPlay = false;
            return null;
        }

        var songInfo = await _mediator.Send(new GetCurrentlyPlayingInfoRequest(_spotifySettings.AuthToken));
        return songInfo;
    }
}