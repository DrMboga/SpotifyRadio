using MediatR;

namespace RadioApp.Common.Messages.Spotify;

public record PausePlaybackRequest(string AuthToken, string DeviceId) : IRequest<bool>;