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
    /// Radio station MyTuner page URL
    /// </summary>
    public string DetailsUrl { get; set; } = string.Empty;
    /// <summary>
    /// Radio station country
    /// </summary>
    public string Country { get; set; } = string.Empty;
    /// <summary>
    /// Radio station region and frequency info
    /// </summary>
    public string? RegionInfo { get; set; }
    /// <summary>
    /// Station rating - sum of likes and dislikes
    /// </summary>
    public int Rating { get; set; }
    /// <summary>
    /// Station URL
    /// </summary>
    public string? StationWebPage { get; set; }
    /// <summary>
    /// Station logo URL
    /// </summary>
    public string? StationImageUrl { get; set; }
    /// <summary>
    /// Music stream URL
    /// </summary>
    public string? StationStreamUrl { get; set; }
}