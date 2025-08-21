using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Spotify;

namespace RadioApp.Common.Messages.Spotify;

public record RefreshSpotifyAuthTokenRequest(SpotifySettings SpotifySettings): IRequest<RefreshTokenResponse>;