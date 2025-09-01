using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record SaveStationsInfosNotification(RadioStationInfo[] Stations) : INotification;