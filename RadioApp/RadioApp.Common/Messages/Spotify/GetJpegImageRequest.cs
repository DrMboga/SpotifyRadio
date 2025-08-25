using MediatR;

namespace RadioApp.Common.Messages.Spotify;

public record GetJpegImageRequest(string ImageUrl) : IRequest<byte[]?>;