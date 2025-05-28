namespace RadioApp.Common.Contracts;

/// <summary>
/// Maps a region from MyTuner databases with a button on a radio panel
/// </summary>
public class RadioRegion
{
    /// <summary>
    /// A button on a radio panel
    /// </summary>
    public SabaRadioButtons SabaRadioButton { get; set; }
    /// <summary>
    /// Region from MyTuner database
    /// </summary>
    public string Region { get; set; } = string.Empty;
}