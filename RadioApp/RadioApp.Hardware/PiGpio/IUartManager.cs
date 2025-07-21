namespace RadioApp.Hardware.PiGpio;

public interface IUartManager
{
    string? ReadUartMessage(int uartHandle);
}