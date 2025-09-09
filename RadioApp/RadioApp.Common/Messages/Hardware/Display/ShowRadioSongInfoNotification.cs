using MediatR;

namespace RadioApp.Common.Messages.Hardware.Display;

public record ShowRadioSongInfoNotification(string SongInfo) : INotification;