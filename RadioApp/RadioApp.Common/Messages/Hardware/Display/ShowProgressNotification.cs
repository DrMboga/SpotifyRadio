using MediatR;

namespace RadioApp.Common.Messages.Hardware.Display;

public record ShowProgressNotification(int PercentComplete): INotification;