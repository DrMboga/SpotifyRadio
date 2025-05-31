using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record SaveRadioStationNotification(RadioStation Station): INotification;