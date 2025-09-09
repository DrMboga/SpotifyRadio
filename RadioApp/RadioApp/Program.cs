using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RadioApp.Common.Hardware;
using RadioApp.Common.MyTunerScraper;
using RadioApp.Common.PlayerProcessor;
using RadioApp.Hardware;
using RadioApp.Hardware.Mock;
using RadioApp.Hardware.PiGpio;
using RadioApp.MyTunerBackgroundScraper;
using RadioApp.Persistence;
using RadioApp.PlayerProcessors;
using RadioApp.RadioController;
using RadioApp.RadioStreaming;
using RadioApp.RadioStreamSettings;
using RadioApp.SpotifySettings;
using Serilog;

const string allowCors = "AllowCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(allowCors,
            policy =>
            {
                policy
                    .WithOrigins("http://localhost:4200", "http://localhost:4200/") // Or .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .AllowAnyMethod()
                    ;
            });
    });
}

//https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio%2Cvisual-studio-code
// /openapi/v1.json
builder.Services.AddOpenApi();

// Configure Serilog
builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
);

// Http client for SpotifyApi
builder.Services.AddHttpClient();

// Data Access
builder.Services.AddDbContextFactory<Persistence>();

RadioStreamingDiSetup.Setup(builder.Services);

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

// Hardware services
var osPlatform = Environment.OSVersion.Platform;
if (osPlatform == PlatformID.Unix)
{
    builder.Services.AddSingleton<IGpioManager, GpioManager>();
    builder.Services.AddSingleton<IUartManager, UartManager>();
    builder.Services.AddSingleton<ISpiManager, SpiManager>();
}
else
{
    builder.Services.AddSingleton<IGpioManager, GpioManagerMock>();
    builder.Services.AddSingleton<IUartManager, UartManagerMock>();
    builder.Services.AddSingleton<ISpiManager, SpiManagerMock>();
}

builder.Services.AddTransient<IHardwareManager, HardwareManager>();
builder.Services.AddTransient<IUartIoListener, UartIoListener>();

// Singleton instance of the Status synchronization service
builder.Services.AddSingleton<RadioStatus>();

// Background worker with IO listening logic
builder.Services.AddHostedService<RadioControllerService>();

// Background worker for MyTuner radio stations info scraping
builder.Services.AddSingleton<MyTunerCachingDispatcher>();
builder.Services.AddHostedService<MyTunerCachingBackgroundService>();

// Player processor factory
builder.Services.AddTransient<IdlePlayerProcessor>();
builder.Services.AddTransient<SpotifyPlayerProcessor>();
builder.Services.AddTransient<InternetRadioPlayerProcessor>();
// Register a delegate which returns IPlayerProcessor instance by PlayerType enum member
builder.Services.AddTransient<PlayerProcessorFactory>(serviceProvider => playerType =>
{
    return playerType switch
    {
        PlayerType.Idle => serviceProvider.GetRequiredService<IdlePlayerProcessor>(),
        PlayerType.Spotify => serviceProvider.GetRequiredService<SpotifyPlayerProcessor>(),
        PlayerType.InternetRadio => serviceProvider.GetRequiredService<InternetRadioPlayerProcessor>(),
        _ => throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null)
    };
});

// Background worker with media playback control
builder.Services.AddHostedService<PlayerProcessorService>();

var app = builder.Build();

// Create database
if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Data", "RadioSettings.db")))
{
    var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data");
    if (!Directory.Exists(dataDirectory))
    {
        Directory.CreateDirectory(dataDirectory);
    }
    
    var dbContextFactory = app.Services.GetService<IDbContextFactory<Persistence>>();
    await using var dbContext = await dbContextFactory!.CreateDbContextAsync();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors(allowCors);
    app.MapOpenApi();
}

if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "browser", "index.html")))
{
    // If Angular app is compiled, adding the default redirect to it
    var browserRootFileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "browser"));
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = browserRootFileProvider,
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = browserRootFileProvider,
    });
    
    // Fallback for Angular App roots
    app.MapFallback(async context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path is "/" or "/spotify" or "/spotify-login" or "/radio")
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(browserRootFileProvider.GetFileInfo("index.html"));
            return;
        }
        
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    });
}

app.UseHttpsRedirection();

app.MapSpotifySettingsEndpoints();
app.MapRadioStreamSettingsEndpoints();

// app.MapSpotifyApiEndpoints();
app.MapScreenApiEndpoints();

// Log file endpoint
app.MapGet("/logs", (IHostEnvironment env) => Results.File(Path.Combine(env.ContentRootPath, "radioService.log"), "text/plain"))
    .WithDescription("Log file");

app.Run();