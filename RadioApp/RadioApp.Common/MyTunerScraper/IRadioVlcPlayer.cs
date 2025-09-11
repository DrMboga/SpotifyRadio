namespace RadioApp.Common.MyTunerScraper;

public interface IRadioVlcPlayer
{
    void Play(string url);
    void Stop();
    string? GetCurrentlyPlaying();
}