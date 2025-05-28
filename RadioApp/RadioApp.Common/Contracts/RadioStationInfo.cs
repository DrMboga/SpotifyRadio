namespace RadioApp.Common.Contracts;

/// <summary>
/// Radio station data from MyTuner database
/// </summary>
public class RadioStationInfo
{
    /// <summary>
    /// Radio station name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// RadioStation frequency
    /// </summary>
    public decimal Frequency { get; set; }
    /// <summary>
    /// Radio station MyTuner page URL
    /// </summary>
    public string DetailsUrl { get; set; } = string.Empty;
    /// <summary>
    /// Radio station region
    /// </summary>
    public string Region { get; set; } = string.Empty;
    /// <summary>
    /// Station URL
    /// </summary>
    public string? StationUrl { get; set; }
    /// <summary>
    /// Station logo URL
    /// </summary>
    public string? StationImageUrl { get; set; }
    /// <summary>
    /// Music stream URL
    /// </summary>
    public string? StationStreamUrl { get; set; }
}