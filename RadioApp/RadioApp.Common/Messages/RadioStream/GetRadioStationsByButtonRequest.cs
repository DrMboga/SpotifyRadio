using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record GetRadioStationsByButtonRequest(SabaRadioButtons Button): IRequest<RadioStation[]>;