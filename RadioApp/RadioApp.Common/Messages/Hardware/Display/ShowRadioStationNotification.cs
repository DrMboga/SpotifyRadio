using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.Hardware.Display;

public record ShowRadioStationNotification(RadioScreenInfo ScreenInfo) : INotification;