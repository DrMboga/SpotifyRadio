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
public class UartIoListener : IUartIoListener, IAsyncDisposable
{
    private const uint InterruptPin = 26;

    private readonly ILogger<UartIoListener> _logger;
    private readonly IHardwareManager _hardwareManager;
    private readonly IGpioManager _gpioManager;
    private readonly IUartManager _uartManager;
    private readonly IMediator _mediator;

    private readonly CancellationTokenSource _cancellationTokenRef = new();
    private Task? _listenToPinInterruptTask;
    private TaskCompletionSource? _interruptPinTriggered;

    public UartIoListener(
        ILogger<UartIoListener> logger,
        IHardwareManager hardwareManager,
        IGpioManager gpioManager,
        IUartManager uartManager,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        _uartManager = uartManager;
        _gpioManager = gpioManager;
        _hardwareManager = hardwareManager;
    }

    public Task StartListenIoChannel(CancellationToken cancellationToken)
    {
        _gpioManager.InitInputPinAsPullUp(InterruptPin);
        _logger.LogDebug("Pin {InterruptPin} set up as input and pull-up", InterruptPin);

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenRef.Token);
        _listenToPinInterruptTask = Task.Run(() => ListenToInterruptPin(linkedCts.Token), linkedCts.Token);
        return Task.CompletedTask;
    }

    private async Task ListenToInterruptPin(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _interruptPinTriggered =
                    new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                // Register one-time callback
                _gpioManager.RegisterPinCallbackFunction(InterruptPin, GpioCallback);
                _logger.LogDebug("--Callback registered");

                // Wait for pin trigger via TaskCompletionSource
                await _interruptPinTriggered.Task.WaitAsync(cancellationToken);
                
                // Delay to ensure UART buffer fills
                await Task.Delay(20, cancellationToken);
                var message = await ReadUartMessage();

                // Unregister one-time callback
                _gpioManager.UnregisterPinCallbackFunction(InterruptPin);
                _logger.LogDebug("--Callback unregistered");

                try
                {
                    // await _mediator.Publish(new NewMessage(message), cancellationToken);
                    _logger.LogDebug("Here we should call Mediator with message '{Message}'", message);
                    // _hardwareManager.SetStatusRequestPin(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing UART message.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message listening canceled.");
        }
    }

    /// <summary>
    /// Callback function triggers when the specified GPIO changes state
    /// </summary>
    private void GpioCallback(int gpio, int level, uint tick)
    {
        if ((GpioLevel)level == GpioLevel.Low && _interruptPinTriggered is { Task.IsCompleted: false } tcs)
        {
            // If interrupt pin is LOW, we are ready to read message from UART
            try
            {
                _logger.LogDebug("tcs.TrySetResult()");
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }
    }

    private async Task<string> ReadUartMessage()
    {
        string? uartMessage = null;
        while (uartMessage == null)
        {
            uartMessage = _uartManager.ReadUartMessage(_hardwareManager.UartHandle);
            if (uartMessage == null)
            {
                await Task.Delay(100);
            }
        }

        _logger.LogDebug("UART Message read '{uartMessage}'", uartMessage);

        return uartMessage;
    }

    #region Dispose

    private void ReleaseUnmanagedResources()
    {
        _gpioManager.UnregisterPinCallbackFunction(InterruptPin);
        _logger.LogInformation("UartIoListener terminated");
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenRef.CancelAsync();
        ReleaseUnmanagedResources();
        if (_listenToPinInterruptTask is not null)
        {
            try
            {
                await _listenToPinInterruptTask;
            }
            catch
            {
                /* Swallow on dispose */
            }
        }

        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }

    ~UartIoListener()
    {
        ReleaseUnmanagedResources();
    }

    #endregion
}