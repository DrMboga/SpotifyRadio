using MediatR;
using RadioApp.Common.Messages.Spotify;

namespace RadioApp.SpotifySettings;

using RadioApp.Common.Contracts;

public static class SpotifySettingsEndpoints
{
    public static void MapSpotifySettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/spotify-settings", (IMediator mediator) => mediator.Send(new GetSpotifySettingsRequest()))
            .WithName("GetSpotifySettings");
        
        app.MapPatch("/spotify-settings", async (SpotifySettings spotifySettings, IMediator mediator) =>
        {
            await mediator.Publish(new SetSpotifySettingsNotification(spotifySettings));
            return spotifySettings;
        })
            .WithName("PatchSpotifySettings")
            .WithDescription("Patching Spotify settings in the DB.");
    }
}