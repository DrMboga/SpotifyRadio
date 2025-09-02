using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record ParseRadioStationRequest(RadioStationInfo RadioStation) : IRequest<RadioStationInfo>;