using RadioApp.Common.Contracts;
using RadioApp.Common.PlayerProcessor;

namespace RadioApp.PlayerProcessors;

public class IdlePlayerProcessor: IPlayerProcessor
{
    public PlayerType Type => PlayerType.Idle;
    
    public Task Start(SabaRadioButtons currentButton, PlayerMode currentPlayerMode, int currentFrequency)
    {
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        return Task.CompletedTask;
    }

    public Task Play()
    {
        return Task.CompletedTask;
    }

    public Task Pause()
    {
        return Task.CompletedTask;
    }

    public Task ToggleButtonChanged(SabaRadioButtons button)
    {
        return Task.CompletedTask;
    }

    public Task FrequencyChanged(int frequency)
    {
        return Task.CompletedTask;
    }
}