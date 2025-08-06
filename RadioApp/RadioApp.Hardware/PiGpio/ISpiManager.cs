namespace RadioApp.Hardware.PiGpio;

/// <summary>
/// Represents SPI hardware communication
/// </summary>
public interface ISpiManager
{
    
    /// <summary>
    /// Sends a command to SPI channel
    /// </summary>
    void SendCommand(int spiHandle, uint rsPin, byte command);

    /// <summary>
    /// Sends data to SPI channel
    /// </summary>
    void SendData(int spiHandle, uint rsPin, byte data);
}