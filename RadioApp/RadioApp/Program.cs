using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RadioApp.Persistence;
using RadioApp.RadioStreamSettings;
using RadioApp.SpotifySettings;

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
                    ;
            });
    });
}

//https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio%2Cvisual-studio-code
// /openapi/v1.json
builder.Services.AddOpenApi();

// Data Access
builder.Services.AddDbContextFactory<Persistence>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

var app = builder.Build();

// Create database
if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "data", "RadioSettings.db")))
{
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