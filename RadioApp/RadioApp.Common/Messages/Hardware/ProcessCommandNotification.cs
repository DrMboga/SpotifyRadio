using MediatR;
using RadioApp.Common.IoCommands;

namespace RadioApp.Common.Messages.Hardware;

public record ProcessCommandNotification(ICommand Command): INotification;