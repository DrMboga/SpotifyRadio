using RadioApp.Common.Hardware;

namespace RadioApp.RadioController;

/// <summary>
/// THis is a background service which listens the UART channel (Pico device).
/// If there is some command from Pico about radio state changes, it sends appropriate command to the Mediator instance.
/// Finally, this command goes to <see cref="RadioStatus"/> singelton instance and delivers to the <see cref="PlayerProcessorService"/>
/// </summary>
public class RadioControllerService: BackgroundService
{
    private readonly ILogger<RadioControllerService> _logger;
    private readonly IHardwareManager _hardwareManager;
    private readonly IUartIoListener _uartIoListener;

    public RadioControllerService(ILogger<RadioControllerService> logger, IHardwareManager hardwareManager, IUartIoListener uartIoListener)
    {
        _logger = logger;
        _hardwareManager = hardwareManager;
        _uartIoListener = uartIoListener;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--== Starting Radio Controller ==--");
        _hardwareManager.Init();
        await _uartIoListener.StartListenIoChannel(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--== Stopping Radio Controller ==--");
        _hardwareManager.Teardown();
        return base.StopAsync(cancellationToken);
    }
}