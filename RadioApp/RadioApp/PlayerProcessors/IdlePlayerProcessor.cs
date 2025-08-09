using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Common.PlayerProcessor;

namespace RadioApp.PlayerProcessors;

public class IdlePlayerProcessor: IPlayerProcessor
{
    private readonly ILogger<IdlePlayerProcessor> _logger;
    private readonly IMediator _mediator;

    public IdlePlayerProcessor(ILogger<IdlePlayerProcessor> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public PlayerType Type => PlayerType.Idle;
    
    public async Task Start(SabaRadioButtons currentButton, PlayerMode currentPlayerMode, int currentFrequency)
    {
        _logger.LogInformation("Starting idle player processor");
        await _mediator.Publish(new InitDisplayNotification());
        await _mediator.Publish(new ClearScreenNotification());
        await _mediator.Publish(new ShowStaticImageNotification("SabaLogo.bmp", 0));
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