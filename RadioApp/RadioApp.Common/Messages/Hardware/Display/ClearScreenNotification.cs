using MediatR;

namespace RadioApp.Common.Messages.Hardware.Display;

public record ClearScreenNotification(): INotification;