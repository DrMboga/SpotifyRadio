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

        app.MapGet("radio-button-region", async (IMediator mediator) =>
        {
            var regionButtonsMap = await mediator.Send(new GetButtonsRegionsRequest());
            return regionButtonsMap;
        }).WithName("RadioRegionByButtonMap");
        
        app.MapPost("radio-button-region", async (RadioRegion regionButton, IMediator mediator) =>
        {
            await mediator.Publish(new RemoveAllRadioStationsByButtonNotification(regionButton.SabaRadioButton));
            await mediator.Publish(new SetButtonRegionNotification(regionButton));
            return regionButton;   
        }).WithName("SetsMappingBetweenButtonAndRegion");
        
        app.MapGet("radio-stations-by-button", async (SabaRadioButtons button, IMediator mediator) =>
        {
            var stationsList = await mediator.Send(new GetRadioStationsByButtonRequest(button));
            return stationsList;
        }).WithName("RadioStationsListConnectedWithButton");

        app.MapPost("radio-stations-by-button", async (RadioStation station, IMediator mediator) =>
        {
            if (station.RadioLogoBase64 == null)
            {
                var radioStationsByRegion = await mediator.Send(new GetRadioStationsListByRegionRequest(station.Region));
                var radioInfo = radioStationsByRegion.FirstOrDefault(s => s.Name == station.Name);
                if (radioInfo?.StationImageUrl != null)
                {
                    var extension = Path.GetExtension(radioInfo.StationImageUrl).Replace(".", "").ToLower();
                    using var webClient = new HttpClient();
                    var imageData = await webClient.GetByteArrayAsync(radioInfo.StationImageUrl);
                    var imageBase64 = Convert.ToBase64String(imageData);
                    station.RadioLogoBase64 = $"data:image/{extension};base64,{imageBase64}";
                }
            }
            await mediator.Publish(new SaveRadioStationNotification(station));
            return station;
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