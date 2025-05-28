using MediatR;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;

namespace RadioApp.RadioStreamSettings;

public static class RadioStreamSettingsEndpoints
{
    public static void MapRadioStreamSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("radio-regions", async (IMediator mediator) =>
        {
            var regions = await mediator.Send(new GetRadioRegionsListRequest());
            return regions;
        }).WithName("RadioRegionsList");
        
        app.MapGet("radio-stations-by-region", async (string region, IMediator mediator) =>
        {
            var radioStations = await mediator.Send(new GetRadioStationsListByRegionRequest(region));
            return radioStations;
        }).WithName("RadioStationsListByRegion");

        app.MapGet("radio-button-region", async () =>
        {
            RadioRegion[] regionButtonsMap =
            [
                new()
                {
                    SabaRadioButton = SabaRadioButtons.L,
                    Region = "Saarland",
                }
            ];
            return regionButtonsMap;
        }).WithName("RadioRegionByButtonMap");
        
        app.MapPost("radio-button-region", async (RadioRegion regionButton) =>
        {
            return regionButton;   
        }).WithName("SetsMappingBetweenButtonAndRegion");
        
        app.MapGet("radio-stations-by-button", async (SabaRadioButtons button) =>
        {
            RadioStation[] stationsList =
            [
                new()
                {
                    Button = button,
                    Name = "Station1",
                    Region = "Saarland",
                    SabaFrequency = (int)button,
                    StreamUrl = "https://spotify.apple.com",
                    RadioLogoBase64 = "Saarland img",
                }
            ];
            return stationsList;
        }).WithName("RadioStationsListConnectedWithButton");

        app.MapPost("radio-stations-by-button", async (RadioStation stationButton) =>
        {
            return stationButton;
        }).WithName("SavesStationInfo");
        
        app.MapGet("radio-buttons", () =>
        {
            SabaRadioButtonInfo[] buttonsToMap =
            [
                new()
                {
                    Button = SabaRadioButtons.L,
                    ButtonLabel = "L",
                    ButtonDescription = "Long waves button which has a L sign on a SABA panel"
                },
                new()
                {
                    Button = SabaRadioButtons.M,
                    ButtonLabel = "M",
                    ButtonDescription = "Middle waves button which has a M sign on a SABA panel"
                },
                new()
                {
                    Button = SabaRadioButtons.K,
                    ButtonLabel = "K",
                    ButtonDescription = "Short waves button which has a K sign on a SABA panel"
                },
                new()
                {
                    Button = SabaRadioButtons.U,
                    ButtonLabel = "U",
                    ButtonDescription = "FM waves button which has an U sign on a SABA panel"
                },
            ];
            return buttonsToMap;
        }).WithName("RadioPanelButtons");
    }
}