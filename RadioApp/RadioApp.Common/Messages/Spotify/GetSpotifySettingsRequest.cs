using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.Spotify;

public record GetSpotifySettingsRequest(): IRequest<SpotifySettings>;