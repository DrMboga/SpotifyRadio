namespace RadioApp.Common.PlayerProcessor;

/// <summary>
/// Creates a player processor object by player type
/// </summary>
public delegate IPlayerProcessor PlayerProcessorFactory(PlayerType playerType);