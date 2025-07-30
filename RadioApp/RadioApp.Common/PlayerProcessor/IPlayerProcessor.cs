using RadioApp.Common.Contracts;

namespace RadioApp.Common.PlayerProcessor;

/// <summary>
/// The radio player processor
/// </summary>
public interface IPlayerProcessor
{
    PlayerType Type { get; }
    /// <summary>
    /// Runs player
    /// </summary>
    Task Start(SabaRadioButtons currentButton, PlayerMode currentPlayerMode, int currentFrequency);
    /// <summary>
    /// Stops player
    /// </summary>
    Task Stop();
    /// <summary>
    /// Play button is pushed 
    /// </summary>
    Task Play();
    /// <summary>
    /// Pause button is pushed
    /// </summary>
    Task Pause();
    /// <summary>
    /// If user changes toggle button, that means we should change the region for internet radio
    /// </summary>
    Task ToggleButtonChanged(SabaRadioButtons button);
    /// <summary>
    /// User changed tuning position
    /// </summary>
    Task FrequencyChanged(int frequency);
}