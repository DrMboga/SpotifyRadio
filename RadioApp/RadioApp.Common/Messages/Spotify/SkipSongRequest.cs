using MediatR;

namespace RadioApp.Common.Messages.Spotify;

public record SkipSongRequest(string AuthToken, string DeviceId, bool SkipToNext) : IRequest<bool>;