using MediatR;

namespace RadioApp.Common.Messages.Hardware.Display;

public record ShowSongInfoRequest(string SongName, string? ArtistName, byte[]? AlbumCoverJpeg) : IRequest<bool>;