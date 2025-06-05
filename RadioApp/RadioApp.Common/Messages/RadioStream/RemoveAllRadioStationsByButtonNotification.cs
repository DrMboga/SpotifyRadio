using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record RemoveAllRadioStationsByButtonNotification(SabaRadioButtons Button): INotification;