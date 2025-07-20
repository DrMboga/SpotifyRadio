namespace RadioApp.Common.Hardware;

public interface IHardwareManager
{
    /// <summary>
    /// Initializes all hardware interfaces like GPIO, UART and SPI
    /// </summary>
    void Init();
    
    /// <summary>
    /// Gracefully releases all hardware resources
    /// </summary>
    void Teardown();
}