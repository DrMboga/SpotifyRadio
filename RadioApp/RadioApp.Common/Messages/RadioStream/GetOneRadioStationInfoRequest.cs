using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record GetOneRadioStationInfoRequest(string Country, string DetailsUrl): IRequest<RadioStationInfo?>;