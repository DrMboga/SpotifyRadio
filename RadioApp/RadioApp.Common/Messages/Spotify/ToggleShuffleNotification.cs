using MediatR;

namespace RadioApp.Common.Messages.Spotify;

public record ToggleShuffleNotification(string AuthToken, string DeviceId) : INotification;