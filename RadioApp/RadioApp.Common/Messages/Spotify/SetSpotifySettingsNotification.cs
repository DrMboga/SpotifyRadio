using MediatR;
using RadioApp.Common.Contracts;

namespace RadioApp.Common.Messages.Spotify;

public record SetSpotifySettingsNotification(SpotifySettings SpotifySettings): INotification;