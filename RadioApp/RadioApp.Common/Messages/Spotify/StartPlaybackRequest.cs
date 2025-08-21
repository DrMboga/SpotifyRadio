using MediatR;

namespace RadioApp.Common.Messages.Spotify;

public record StartPlaybackRequest(string AuthToken, string DeviceId, string PlaylistId) : IRequest<bool>;