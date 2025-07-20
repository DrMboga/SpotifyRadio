using System.Runtime.InteropServices;
using System.Text;

namespace RadioApp.Hardware.PiGpio;

public static class UartManager
{
    public static string? ReadUartMessage(int uartHandle)
    {
        var messageArrived = false;
        const uint bufferSize = 256;
        var buffer = Marshal.AllocHGlobal((int)bufferSize);
        var bytesRead = PiGpioInterop.serRead((uint)uartHandle, buffer, bufferSize);
        var message = string.Empty;
        if (bytesRead > 0)
        {
            var messageAsBytes = new byte[bytesRead];
            Marshal.Copy(buffer, messageAsBytes, 0, bytesRead);
            message = Encoding.UTF8.GetString(messageAsBytes);

            messageArrived = true;
        }
        Marshal.FreeHGlobal(buffer);
        return messageArrived ? message : null;
    }
}