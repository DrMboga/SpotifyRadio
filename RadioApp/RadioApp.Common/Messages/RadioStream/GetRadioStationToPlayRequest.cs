using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record GetRadioStationToPlayRequest(SabaRadioButtons Button, int Frequency) : IRequest<RadioStation?>;