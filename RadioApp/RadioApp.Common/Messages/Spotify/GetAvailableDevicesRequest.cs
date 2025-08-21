using MediatR;
using RadioApp.Common.Spotify;

namespace RadioApp.Common.Messages.Spotify;

public record GetAvailableDevicesRequest(string AuthToken): IRequest<AvailableDevicesResponse[]>;