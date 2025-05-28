namespace RadioApp.SpotifySettings;

using RadioApp.Common.Contracts;

public static class SpotifySettingsEndpoints
{
    public static void MapSpotifySettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/spotify-settings", async () =>
        {
            return new SpotifySettings();
        }).WithName("GetSpotifySettings");
        
        app.MapPatch("/spotify-settings", async (SpotifySettings spotifySettings) =>
        {
            return spotifySettings;
        })
            .WithName("PatchSpotifySettings")
            .WithDescription("Patching Spotify settings in the DB.");
    }
}