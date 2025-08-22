using MediatR;
using RadioApp.Common.Spotify;

namespace RadioApp.Common.Messages.Spotify;

/// <param name="Market">An ISO 3166-1 alpha-2 country code associated with Spotify account. In my case this is Germany.</param>
public record GetCurrentlyPlayingInfoRequest(string AuthToken, string Market = "DE"): IRequest<SongInfoResponse?>;