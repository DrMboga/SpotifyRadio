namespace RadioApp.Common.Contracts;

/// <summary>
/// Represents a physical buttons on the radio panel
/// </summary>
public enum SabaRadioButtons: short
{
    /// <summary>
    /// Play/Pause button which called Auto on a SABA panel
    /// </summary>
    PlayPause = 0,

    /// <summary>
    /// Long waves button which has a L sign on a SABA panel
    /// </summary>
    L = 1,
  
    /// <summary>
    /// Middle waves button which has a M sign on a SABA panel
    /// </summary>
    M = 2,
    
    /// <summary>
    /// Short waves button which has a K sign on a SABA panel
    /// </summary>
    K = 3,
    
    /// <summary>
    /// FM waves button which has an U sign on a SABA panel
    /// </summary>
    U = 4,
}

// https://stackoverflow.com/a/1799401/28360085