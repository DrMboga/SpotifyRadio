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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

//https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-9.0&tabs=visual-studio%2Cvisual-studio-code
// /openapi/v1.json
builder.Services.AddOpenApi();

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