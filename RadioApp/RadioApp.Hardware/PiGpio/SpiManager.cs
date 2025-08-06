using System.Runtime.InteropServices;

namespace RadioApp.Hardware.PiGpio;

public class SpiManager: ISpiManager
{
    public void SendCommand(int spiHandle, uint rsPin, byte command)
    {
        PiGpioInterop.gpioWrite(rsPin, (uint)GpioLevel.Low); // Command mode

        IntPtr commandPointer = Marshal.AllocHGlobal(1);
        Marshal.WriteByte(commandPointer, command);

        int writeResult = PiGpioInterop.spiWrite((uint)spiHandle, commandPointer, 1);

        Marshal.FreeHGlobal(commandPointer);
        if(writeResult < 0)
        {
            throw new GpioException($"Failed to write SPI Command '{command}'", writeResult);
        }
    }

    public void SendData(int spiHandle, uint rsPin, byte data)
    {
        PiGpioInterop.gpioWrite(rsPin, (uint)GpioLevel.High); // Data mode

        IntPtr dataPointer = Marshal.AllocHGlobal(1);
        Marshal.WriteByte(dataPointer, data);

        int writeResult = PiGpioInterop.spiWrite((uint)spiHandle, dataPointer, 1);
        Marshal.FreeHGlobal(dataPointer);

        if(writeResult < 0)
        {
            throw new GpioException($"Failed to write SPI Data '{data}'", writeResult);
        }
    }
}