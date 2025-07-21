namespace RadioApp.Common.Hardware;

public interface IHardwareManager
{
    /// <summary>
    /// Keeps the handle of UART channel
    /// </summary>
    int UartHandle { get; }
    
    /// <summary>
    /// Initializes all hardware interfaces like GPIO, UART and SPI
    /// </summary>
    void Init();
    
    /// <summary>
    /// Gracefully releases all hardware resources
    /// </summary>
    void Teardown();

    /// <summary>
    /// When we need to request current IO status (button pushed and frequency set),
    /// we have to set the appropriate output pin as LOW
    /// This will be interpreted by PICO as signal and PICO will send to the UART its status.
    /// When command from PICO received, we have to set pin back to HIGH 
    /// </summary>
    /// <param name="isRequestStatus">
    ///     - true to set pin LOW (request status mode),
    ///     - false to set pin to HIGH (we don't need status anymore)
    /// </param>
    void SetStatusRequestPin(bool isRequestStatus);
}