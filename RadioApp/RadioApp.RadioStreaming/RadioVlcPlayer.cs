using LibVLCSharp.Shared;
using RadioApp.Common.MyTunerScraper;

namespace RadioApp.RadioStreaming;

public class RadioVlcPlayer : IRadioVlcPlayer, IDisposable
{
    private LibVLC? _libVlc = null;
    private Media? _media = null;
    private MediaPlayer? _mediaPlayer = null;

    private readonly object _vlcSync = new object();

    public RadioVlcPlayer()
    {
        // Initialize LibVLC
        Core.Initialize();
    }

    public void Play(string url)
    {
        lock (_vlcSync)
        {
            _libVlc = new LibVLC();
            _media = new Media(_libVlc, url, FromType.FromLocation);
            _mediaPlayer = new MediaPlayer(_media);
            _mediaPlayer.Volume = 30;
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