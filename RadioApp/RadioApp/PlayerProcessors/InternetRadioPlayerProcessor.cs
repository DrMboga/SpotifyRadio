using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.PlayerProcessor;

namespace RadioApp.PlayerProcessors;

public class InternetRadioPlayerProcessor: IPlayerProcessor
{
    public PlayerType Type => PlayerType.InternetRadio;
    
    private readonly ILogger<InternetRadioPlayerProcessor> _logger;
    private readonly IMediator _mediator;
    
    public InternetRadioPlayerProcessor(ILogger<InternetRadioPlayerProcessor> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }
    
    public async Task Start(SabaRadioButtons currentButton, PlayerMode currentPlayerMode, int currentFrequency)
    {
        _logger.LogInformation("Starting Internet radio player processor");
        await _mediator.Publish(new ClearScreenNotification());
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