using MediatR;
using RadioApp.Common.Spotify;

namespace RadioApp.Common.Messages.Spotify;

public record GetSpotifyPlaylistsRequest(string AuthToken) : IRequest<PlaylistItem[]>;