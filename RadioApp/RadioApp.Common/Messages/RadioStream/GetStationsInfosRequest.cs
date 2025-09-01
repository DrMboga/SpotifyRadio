using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record GetStationsInfosRequest(string Country) : IRequest<RadioStationInfo[]>;