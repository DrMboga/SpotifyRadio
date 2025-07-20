using RadioApp.Common.Hardware;

namespace RadioApp.RadioController;

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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--== Starting Radio Controller ==--");
        _hardwareManager.Init();
        _uartIoListener.StartListenIoChannel();
            
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("--== Stopping Radio Controller ==--");
        _hardwareManager.Teardown();
        return base.StopAsync(cancellationToken);
    }
}