using MediatR;
using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware;

/// <summary>
/// This class is responsible for IO communication with Raspberry Pi Pico
/// When radio button state or radio frequency changed, PICO lowers down the interrupt pin
/// That means that the new message will be sent via UART
/// This class subscribes to pin state change. While pin state is HIGH, nothing happens, thread is sleeping
/// When interrupt happens, thead wakes up and starts to read from UART
/// When message read, the appropriate command is sent to the Mediator
/// </summary>
public class UartIoListener: IUartIoListener, IDisposable
{
    private const uint InterruptPin = 26;
    
    private readonly ILogger<UartIoListener> _logger;
    private readonly IHardwareManager  _hardwareManager;
    private readonly IMediator _mediator;
    
    private readonly ManualResetEvent _interruptHandleDone = new ManualResetEvent(false);
    private readonly Thread _interruptListenerThread;
    
    private bool _disposed = false;
    
    public UartIoListener(ILogger<UartIoListener> logger, IHardwareManager hardwareManager, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        _hardwareManager = hardwareManager;
        _interruptListenerThread = new Thread(ListenToInterruptPin);
    }

    public void StartListenIoChannel()
    {
        GpioManager.InitInputPinAsPullUp(InterruptPin);
        _logger.LogDebug("Pin {InterruptPin} set up as input and pull-up", InterruptPin);
        _interruptListenerThread.Start();
    }

    private void ListenToInterruptPin()
    {
        while (!_disposed)
        {
            GpioManager.RegisterPinCallbackFunction(InterruptPin, GpioCallback);
            _interruptHandleDone.WaitOne();
            GpioManager.UnregisterPinCallbackFunction(InterruptPin);
            _interruptHandleDone.Reset();
        }
    }
    
    /// <summary>
    /// Callback function triggers when the specified GPIO changes state
    /// </summary>
    private void GpioCallback(int gpio, int level, uint tick)
    {
        if ((GpioLevel)level == GpioLevel.Low)
        {
            ReadUartMessage();
        }
    }

    private void ReadUartMessage()
    {
        string? uartMessage = null;
        while (true)
        {
            uartMessage = UartManager.ReadUartMessage(_hardwareManager.UartHandle);
            if (uartMessage != null)
            {
                break;
            }
            Thread.Sleep(100);
        }
        _logger.LogDebug("UART Message read '{uartMessage}'", uartMessage);

        // TODO: call here: await _mediator.Send(new NewMessage(uartMessage));

        _interruptHandleDone.Set();
    }
    
    #region Dispose
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        GpioManager.UnregisterPinCallbackFunction(InterruptPin);
        _logger.LogInformation("UartIoListener terminated");

        Interlocked.Exchange(ref _disposed, true);
        _interruptListenerThread.Interrupt();
    }

    ~UartIoListener()
    {
        Dispose(disposing: false);
    }
    #endregion
}