namespace RadioApp.Common.Contracts;

/// <summary>
/// Radio station info to play on a radio device
/// </summary>
public class RadioStation
{
    public SabaRadioButtons Button { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SabaFrequency { get; set; }
    public string? StreamUrl { get; set; } = string.Empty;
    public string? RadioLogoBase64 { get; set; }
}