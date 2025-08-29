using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record GetMyTunerStationsRequest(MyTunerStationsRequest CountryInfo) : IRequest<RadioStationInfo[]>;