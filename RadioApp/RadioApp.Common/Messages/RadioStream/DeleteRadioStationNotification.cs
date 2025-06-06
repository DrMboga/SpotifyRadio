using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record DeleteRadioStationNotification(SabaRadioButtons Button, int SabaFrequency): INotification;