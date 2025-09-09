using MediatR;
using Microsoft.AspNetCore.Mvc;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.Hardware.Display;

namespace RadioApp.RadioStreamSettings;

public static class ScreenApiEndpoints
{
    [Obsolete("This is temporary endpoint just to check Screen in debug mode")]
    public static void MapScreenApiEndpoints(this WebApplication app)
    {
        app.MapPost("screen-api-show-frequency-info",
                async (IMediator mediator, ILogger<Program> logger, [FromQuery] string message) =>
                {
                    logger.LogDebug($"Showing '{message}'");
                    await mediator.Publish(new ClearScreenNotification());
                    await mediator.Publish(new ShowFrequencyInfoNotification(message));
                })
            .WithDescription("Shows a sting in the upper right corner of the screen");

        app.MapPost("screen-api-show-station-info",
                async (IMediator mediator, ILogger<Program> logger, [FromBody] RadioScreenInfo screenInfo) =>
                {
                    await mediator.Publish(new ShowRadioStationNotification(screenInfo));
                })
            .WithDescription("Shows a station info");

        app.MapPost("screen-api-show-song-info",
                async (IMediator mediator, ILogger<Program> logger, [FromQuery] string message) =>
                {
                    await mediator.Publish(new ShowRadioSongInfoNotification(message));
                })
            .WithDescription("Shows a song info");
    }
}