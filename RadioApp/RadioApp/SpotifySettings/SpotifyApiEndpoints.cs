using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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

                    logger.LogDebug(
                        $"Token Expiration: {spotifySettings.AuthTokenExpiration}; Now: {now}; Is token expired: {tokenExpired}");
                    if (!tokenExpired)
                    {
                        return Results.Ok("No need to prolong, token is still valid");
                    }

                    var refreshedToken = await mediator.Send(new RefreshSpotifyAuthTokenRequest(spotifySettings));
                    logger.LogDebug($"Refreshed Token: {JsonSerializer.Serialize(refreshedToken)}");

                    if (string.IsNullOrEmpty(refreshedToken.AccessToken))
                    {
                        return Results.BadRequest("Something went wrong...");
                    }

                    spotifySettings.AuthToken = refreshedToken.AccessToken;
                    if (!string.IsNullOrEmpty(refreshedToken.RefreshToken))
                    {
                        spotifySettings.RefreshToken = refreshedToken.RefreshToken;
                    }

                    spotifySettings.AuthTokenExpiration = now + refreshedToken.ExpiresIn * 1000;

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

        app.MapGet("/spotify-api-playlists", async (IMediator mediator, ILogger<Program> logger) =>
            {
                var spotifySettings = await mediator.Send(new GetSpotifySettingsRequest());
                var playlists = await mediator.Send(new GetSpotifyPlaylistsRequest(spotifySettings.AuthToken));
                return playlists;
            })
            .WithName("Spotify API get playlists request")
            .WithDescription("Temporary method to get playlists");

        app.MapPut("/spotify-api-start-playback", async (IMediator mediator, ILogger<Program> logger,
                [FromQuery] string device, [FromQuery] string playlist, [FromQuery] bool resume) =>
            {
                var spotifySettings = await mediator.Send(new GetSpotifySettingsRequest());
                logger.LogDebug($"Playing '{playlist}' playlist on '{device}' device");
                var success =
                    await mediator.Send(new StartPlaybackRequest(spotifySettings.AuthToken, device, playlist, resume));
                if (!resume)
                {
                    await mediator.Publish(new ToggleShuffleNotification(spotifySettings.AuthToken, device));
                }

                return success ? Results.Ok("Playback started") : Results.BadRequest("Failed to start playback");
            })
            .WithName("Spotify API starts playback")
            .WithDescription("Temporary method to start playback");
        
        app.MapPut("/spotify-api-pause-playback", async (IMediator mediator, ILogger<Program> logger, [FromQuery] string device) =>
            {
                var spotifySettings = await mediator.Send(new GetSpotifySettingsRequest());
                logger.LogDebug($"Pause on '{device}' device");
                var success = await mediator.Send(new PausePlaybackRequest(spotifySettings.AuthToken,device));
                return success ? Results.Ok("Playback paused") : Results.BadRequest("Failed to pause playback");
            })
            .WithName("Spotify API pauses playback")
            .WithDescription("Temporary method to pause playback");
    }
}