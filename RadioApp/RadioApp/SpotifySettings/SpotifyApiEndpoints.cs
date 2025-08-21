using System.Text.Json;
using MediatR;
using RadioApp.Common.Messages.Spotify;

namespace RadioApp.SpotifySettings;

public static class SpotifyApiEndpoints
{
    [Obsolete("This is temporary endpoint just to check Spotify API in debug mode")]
    public static void MapSpotifyApiEndpoints(this WebApplication app)
    {
        app.MapPost("/spotify-api-refresh-token-if-needed",
                async (IMediator mediator, ILogger<Program> logger) =>
                {
                    var spotifySettings = await mediator.Send(new GetSpotifySettingsRequest());
                    if (spotifySettings?.AuthToken == null || spotifySettings?.RefreshToken == null ||
                        spotifySettings?.AuthTokenExpiration == null)
                    {
                        logger.LogError("Spotify API refresh token is missing");
                        return Results.BadRequest("Spotify API refresh token is missing");
                    }

                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var tokenExpired = now > spotifySettings.AuthTokenExpiration;
                    
                    logger.LogDebug($"Token Expiration: {spotifySettings.AuthTokenExpiration}; Now: {now}; Is token expired: {tokenExpired}");
                    if (!tokenExpired)
                    {
                        return Results.Ok("No need to prolong, token is still valid");
                    }
                    
                    var refreshedToken = await mediator.Send(new RefreshSpotifyAuthTokenRequest(spotifySettings));
                    logger.LogDebug($"Refreshed Token: {JsonSerializer.Serialize(refreshedToken)}");

                    if (string.IsNullOrEmpty(refreshedToken.AccessToken) ||
                        string.IsNullOrEmpty(refreshedToken.RefreshToken))
                    {
                        return Results.BadRequest("Something went wrong...");
                    }
                    
                    spotifySettings.AuthToken = refreshedToken.AccessToken;
                    spotifySettings.RefreshToken = refreshedToken.RefreshToken;
                    spotifySettings.AuthTokenExpiration = now + refreshedToken.ExpiresIn*1000;
                    
                    await mediator.Publish(new SetSpotifySettingsNotification(spotifySettings));

                    return Results.Ok("New Auth token saved");
                })
            .WithName("Spotify API Refresh Token if needed")
            .WithDescription("Temporary method to check refresh token logic");

        app.MapGet("/spotify-api-available-devices", async (IMediator mediator, ILogger<Program> logger) =>
        {
            var spotifySettings = await mediator.Send(new GetSpotifySettingsRequest());
            var devices = await mediator.Send(new GetAvailableDevicesRequest(spotifySettings.AuthToken));
            return devices;
        })
        .WithName("Spotify API get devices request")
        .WithDescription("Temporary method to get available devices list");
    }
}