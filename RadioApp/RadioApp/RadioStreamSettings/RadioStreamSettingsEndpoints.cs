using MediatR;
using Microsoft.AspNetCore.Mvc;
using RadioApp.Common.Contracts;
using RadioApp.Common.Messages.RadioStream;

namespace RadioApp.RadioStreamSettings;

public static class RadioStreamSettingsEndpoints
{
    public static void MapRadioStreamSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("radio-countries-list", async (IMediator mediator) =>
        {
            var countries = await mediator.Send(new GetMyTunerCountriesListRequest());
            return countries;
        }).WithName("List of countries from MyTuner");

        app.MapGet("radio-stations-by-country",
                async (IMediator mediator, [FromBody] MyTunerStationsRequest country) =>
                {
                    var stations = await mediator.Send(new GetMyTunerStationsRequest(country));
                    return stations;
                })
            .WithName("List of stations by country sorted by rating");

        app.MapGet("radio-stations-by-button", async (SabaRadioButtons button, IMediator mediator) =>
        {
            var stationsList = await mediator.Send(new GetRadioStationsByButtonRequest(button));
            return stationsList;
        }).WithName("RadioStationsListConnectedWithButton");

        app.MapPost("radio-stations-by-button", async (RadioStation station, IMediator mediator) =>
        {
            if (station.RadioLogoBase64 == null)
            {
                // TODO: Find another way of saving image of the station
                // if (radioInfo?.StationImageUrl != null)
                // {
                //     var extension = Path.GetExtension(radioInfo.StationImageUrl).Replace(".", "").ToLower();
                //     using var webClient = new HttpClient();
                //     var imageData = await webClient.GetByteArrayAsync(radioInfo.StationImageUrl);
                //     var imageBase64 = Convert.ToBase64String(imageData);
                //     station.RadioLogoBase64 = $"data:image/{extension};base64,{imageBase64}";
                // }
            }

            await mediator.Publish(new SaveRadioStationNotification(station));
            return station;
        }).WithName("SavesStationInfo");

        app.MapDelete("radio-stations-by-button",
            async (SabaRadioButtons button, int sabaFrequency, IMediator mediator) =>
            {
                await mediator.Publish(new DeleteRadioStationNotification(button, sabaFrequency));
            }).WithName("DeleteStationInfo");

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