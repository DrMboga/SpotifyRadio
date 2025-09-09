using MediatR;

namespace RadioApp.Common.Messages.Hardware.Display;

/// <summary>
/// Shows a text in the upper right corner of the screen
/// </summary>
/// <param name="FrequencyInfo"></param>
public record ShowFrequencyInfoNotification(string FrequencyInfo) : INotification;