namespace RadioApp.Common.Hardware;

/// <summary>
/// Responsible for getting messages from Raspberry Pi Pico.
/// </summary>
public interface IUartIoListener
{
    /// <summary>
    /// Initializes the interrupt pin and starts to receive messages from UART protocol.
    /// When any message arrives, sends appropriate commands to Mediator
    /// </summary>
    void StartListenIoChannel();
}