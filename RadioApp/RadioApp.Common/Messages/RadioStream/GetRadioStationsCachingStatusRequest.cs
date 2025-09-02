using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.RadioStream;

public record GetRadioStationsCachingStatusRequest(string Country) : IRequest<MyTunerCachingStatus>;