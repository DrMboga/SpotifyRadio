using MediatR;
using RadioApp.Common.IoCommands;
using RadioApp.Common.Messages.Hardware;

namespace RadioApp.RadioController;

public class CommandsProcessor : INotificationHandler<ProcessCommandNotification>
{
    private readonly ILogger<CommandsProcessor> _logger;
    private readonly RadioStatus _radioStatus;

    public CommandsProcessor(ILogger<CommandsProcessor> logger, RadioStatus radioStatus)
    {
        _logger = logger;
        _radioStatus = radioStatus;
    }

    public Task Handle(ProcessCommandNotification notification, CancellationToken cancellationToken)
    {
        return _radioStatus.HandleIoCommand(notification.Command);
    }
}