using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RadioApp.Common.Hardware;
using RadioApp.Hardware;
using RadioApp.Hardware.Mock;
using RadioApp.Persistence;
using RadioApp.RadioController;
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


// Data Access
builder.Services.AddDbContextFactory<Persistence>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

// Hardware services
var osPlatform = Environment.OSVersion.Platform;
if (osPlatform == PlatformID.Unix)
{
    builder.Services.AddSingleton<IHardwareManager, HardwareManager>();
    builder.Services.AddSingleton<IUartIoListener, UartIoListener>();
}
else
{
    builder.Services.AddSingleton<IHardwareManager, HardwareManagerMock>();
    builder.Services.AddSingleton<IUartIoListener, UartIoListenerMock>();
}

// Background worker with main radio logic
builder.Services.AddHostedService<RadioControllerService>();

var app = builder.Build();

// Create database
if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "data", "RadioSettings.db")))
{
    var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
    Directory.CreateDirectory(dataDirectory);
    
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
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "browser")),
        RequestPath = ""
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "browser")),
        RequestPath = ""
    });
}

app.UseHttpsRedirection();

app.MapSpotifySettingsEndpoints();
app.MapRadioStreamSettingsEndpoints();

app.Run();