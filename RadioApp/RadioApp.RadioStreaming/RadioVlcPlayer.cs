using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using RadioApp.Common.MyTunerScraper;

namespace RadioApp.RadioStreaming;

public class RadioVlcPlayer : IRadioVlcPlayer, IDisposable
{
    private LibVLC? _libVlc = null;
    private Media? _media = null;
    private MediaPlayer? _mediaPlayer = null;

    private readonly object _vlcSync = new object();

    private readonly ILogger<RadioVlcPlayer> _logger;

    public RadioVlcPlayer(ILogger<RadioVlcPlayer> logger)
    {
        _logger = logger;
        // Initialize LibVLC
        Core.Initialize();
    }

    public void Play(string url)
    {
        if (_libVlc != null)
        {
            return;
        }
        lock (_vlcSync)
        {
            _libVlc = new LibVLC(
                "--no-video",
                "--aout=alsa", // force ALSA instead of Pulse
                "--alsa-audio-device=default",
                "--quiet");
            _media = new Media(_libVlc, url, FromType.FromLocation);
            _mediaPlayer = new MediaPlayer(_media);
            _mediaPlayer.Volume = 30;
            _logger.LogDebug("LibVLC instance created");
        }

        _mediaPlayer?.Play();
    }

    public void Stop()
    {
        if (_mediaPlayer == null)
        {
            return;
        }

        _mediaPlayer.Stop();

        lock (_vlcSync)
        {
            _libVlc?.Dispose();
            _media?.Dispose();
            _mediaPlayer.Dispose();

            _libVlc = null;
            _media = null;
            _mediaPlayer = null;
            _logger.LogDebug("LibVLC disposed");
        }
    }

    public string? GetCurrentlyPlaying()
    {
        return _mediaPlayer?.Media?.Meta(MetadataType.NowPlaying);
    }

    public void Dispose()
    {
        Stop();
    }
}