using System.Reflection;
using Microsoft.Extensions.FileProviders;
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

// MediatR
Assembly[] mediatRAssemblies = [ 
    Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RadioApp.Persistence.dll")) 
];
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(mediatRAssemblies));

var app = builder.Build();

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