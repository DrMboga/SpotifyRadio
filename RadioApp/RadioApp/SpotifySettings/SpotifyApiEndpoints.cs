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

                    return Results.Ok();
                })
            .WithName("Spotify API Refresh Token if needed")
            .WithDescription("Temporary method to check refresh token logic");
    }
}