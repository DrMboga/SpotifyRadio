using MediatR;

namespace RadioApp.Common.Messages.RadioStream;

public record GetRadioRegionsListRequest(): IRequest<string[]>;