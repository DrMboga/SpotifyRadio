using MediatR;

namespace RadioApp.Common.Messages.Hardware.Display;

/// <summary>
/// Loads a bmp file from Assets folder and shows it on the display
/// </summary>
/// <param name="AssetName">Bmp file name without path</param>
public record ShowStaticImageNotification(string AssetName): INotification;