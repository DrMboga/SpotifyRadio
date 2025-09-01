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
    /// Station rating
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Likes count
    /// </summary>
    public int Likes { get; set; }

    /// <summary>
    /// Dislikes count
    /// </summary>
    public int Dislikes { get; set; }

    /// <summary>
    /// Station description
    /// </summary>
    public string StationDescription { get; set; } = string.Empty;

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